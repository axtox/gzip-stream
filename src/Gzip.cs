using System;
using System.IO;
using System.Threading;
using System.Diagnostics;
using System.IO.Compression;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace GZipStream
{
	/*
	 * Класс для работы в мультипроцессорной
	 * среде. Один поток отвечает за чтение, другой
	 * сразу записывает полученный блок байтов.
	 */
	class MultiThreadGZip
	{
		/*
		 * Объект синхронизации - в очередь
		 * добавляются блоки байтов по 4096 байт,
		 * затем второй поток исключает один блок
		 * из очереди для дальнейшей конвертации.
		 */
		public static Queue<byte[]> crossWriteBuffer = new Queue<byte[]>();
		public static bool reachedEnd = false;

		/*
		 * Чтение файла и запись считанного блока
		 * в синхронизированную очередь, для дальнейшей
		 * обработки в writeCompressed.
		 * Остальные методы работают по аналогии.
		 */
		public void doRead(string pathToFile) {
			using (var fsInput = new FileStream(pathToFile, FileMode.Open, FileAccess.Read)) {
				var localBuffer = new Byte[Constants.bufferSize];
				int temp;
				do {
					lock (crossWriteBuffer) {
						temp = fsInput.Read(localBuffer, 0, localBuffer.Length);
						enqueueBuffer(localBuffer);
						Monitor.Pulse(crossWriteBuffer);	//Дать сигнал о готовности блока к записи
						Monitor.Wait(crossWriteBuffer);		//Ждать сигнала о записи очередного блока
					}
				} while (temp > 0);
				setReachedEndStatus(true);
			}
		}

		public void writeCompressed(string destinationPath) {
			using (var fsOutput = new FileStream(destinationPath, FileMode.Create, FileAccess.Write))
			using (var gzipStream = new GZipStream(fsOutput, CompressionMode.Compress)) {
				while (!reachedEnd || crossWriteBuffer.Count != 0) {
					lock (crossWriteBuffer) {
						var localBuffer = dequeueBuffer();
						gzipStream.Write(localBuffer, 0, localBuffer.Length);
						Monitor.Pulse(crossWriteBuffer);					//Дать сигнал о записи блока
						if (!reachedEnd) Monitor.Wait(crossWriteBuffer);	//Ждать сигнала о готовности очередного блока (если не был достигнут конец файла)
					}
				}
			}
		}

		public void doReadCompressed(string pathToCompressedFile) {
			using (var fsInput = new FileStream(pathToCompressedFile, FileMode.Open, FileAccess.Read))
			using (var gzipStream = new GZipStream(fsInput, CompressionMode.Decompress)) {
				var buffer = new byte[Constants.bufferSize];
				int temp;
				do {
					lock (crossWriteBuffer) {
						temp = gzipStream.Read(buffer, 0, buffer.Length);
						enqueueBuffer(buffer);
						Monitor.Pulse(crossWriteBuffer);
						Monitor.Wait(crossWriteBuffer);
					}
				} while (temp > 0);
			}
			setReachedEndStatus(true);
		}

		public void writeDecompressed(string destinationPath) {
			using (var fsOutput = new FileStream(destinationPath, FileMode.Create, FileAccess.Write)) {
				while (!reachedEnd || crossWriteBuffer.Count != 0) {
					lock (crossWriteBuffer) {
						var localBuffer = dequeueBuffer();
						fsOutput.Write(localBuffer, 0, localBuffer.Length);
						Monitor.Pulse(crossWriteBuffer);
						if (!reachedEnd) Monitor.Wait(crossWriteBuffer);
					}
				}
			}
		}

		/*
		 * Полностью синхронизированный метод.
		 * При срабатывании блокирует весь тип
		 * т.к. относится к static
		 */
		[MethodImplAttribute(MethodImplOptions.Synchronized)]
		static void enqueueBuffer(byte[] buffer) {
			crossWriteBuffer.Enqueue(buffer);
		}
		[MethodImplAttribute(MethodImplOptions.Synchronized)]
		static byte[] dequeueBuffer() {
			return crossWriteBuffer.Dequeue();
		}

		/*
		 * Устанавливает значение reachedEnd в сигнальное
		 * состояние. Используется для подачи сигнала
		 * потокам об окончании чтения из файла.
		 */
		static void setReachedEndStatus(bool setStatus) {
			lock (crossWriteBuffer) {
				reachedEnd = setStatus;
				Monitor.Pulse(crossWriteBuffer);	//подает последний сигнал пишущему потоку
			}
		}
	}
				
	/*
	 * Класс, для конвертирования в однопроцессорной
	 * среде.
	 */
    class DefaultGZip
    {
        public static void doCompress(String fileSource, String fileDestination) {
            using (var fsInput = new FileStream(fileSource, FileMode.Open, FileAccess.Read))
                using (var fsOutput = new FileStream(fileDestination, FileMode.Create, FileAccess.Write))
                    using (var gzipStream = new GZipStream(fsOutput, CompressionMode.Compress)) {
                        var buffer = new Byte[Constants.bufferSize];
						int temp;
						while ((temp = fsInput.Read(buffer, 0, buffer.Length)) > 0) {
							gzipStream.Write(buffer, 0, temp);
                        }
                    }
        }

		public static void doDecompress(String fileSource, String fileDestination) {
			using (var fsInput = new FileStream(fileSource, FileMode.Open, FileAccess.Read)) 
				using (var fsOutput = new FileStream(fileDestination, FileMode.Create, FileAccess.Write)) 
					using (var gzipStream = new GZipStream(fsInput, CompressionMode.Decompress)) {
						var buffer = new Byte[Constants.bufferSize];
						int temp;
						while ((temp = gzipStream.Read(buffer, 0, buffer.Length)) > 0) {
							fsOutput.Write(buffer, 0, temp);
						}
					}
		}
    }
}