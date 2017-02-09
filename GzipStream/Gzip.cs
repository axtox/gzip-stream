using System;
using System.IO;
using System.Threading;
using System.Diagnostics;
using System.IO.Compression;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace GzipStreamDemo
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
		public static Queue<byte[]> CrossWriteBuffer = new Queue<byte[]>();
		public static bool ReachedEnd = false;

		/*
		 * Чтение файла и запись считанного блока
		 * в синхронизированную очередь, для дальнейшей
		 * обработки в writeCompressed.
		 * Остальные методы работают по аналогии.
		 */
		public void DoRead(string pathToFile) {
			using (var fsInput = new FileStream(pathToFile, FileMode.Open, FileAccess.Read)) {
				var localBuffer = new byte[Constants.BufferSize];
				int temp;
				do {
					lock (CrossWriteBuffer) {
						temp = fsInput.Read(localBuffer, 0, localBuffer.Length);
						EnqueueBuffer(localBuffer);
						Monitor.Pulse(CrossWriteBuffer);	//Дать сигнал о готовности блока к записи
						Monitor.Wait(CrossWriteBuffer);		//Ждать сигнала о записи очередного блока
					}
				} while (temp > 0);
				SetReachedEndStatus(true);
			}
		}

		public void WriteCompressed(string destinationPath) {
			using (var fsOutput = new FileStream(destinationPath, FileMode.Create, FileAccess.Write))
			using (var gzipStream = new System.IO.Compression.GZipStream(fsOutput, CompressionMode.Compress)) {
				while (!ReachedEnd || CrossWriteBuffer.Count != 0) {
					lock (CrossWriteBuffer) {
						var localBuffer = DequeueBuffer();
						gzipStream.Write(localBuffer, 0, localBuffer.Length);
						Monitor.Pulse(CrossWriteBuffer);					//Дать сигнал о записи блока
						if (!ReachedEnd) Monitor.Wait(CrossWriteBuffer);	//Ждать сигнала о готовности очередного блока (если не был достигнут конец файла)
					}
				}
			}
		}

		public void DoReadCompressed(string pathToCompressedFile) {
			using (var fsInput = new FileStream(pathToCompressedFile, FileMode.Open, FileAccess.Read))
			using (var gzipStream = new GZipStream(fsInput, CompressionMode.Decompress)) {
				var buffer = new byte[Constants.BufferSize];
				int temp;
				do {
					lock (CrossWriteBuffer) {
						temp = gzipStream.Read(buffer, 0, buffer.Length);
						EnqueueBuffer(buffer);
						Monitor.Pulse(CrossWriteBuffer);
						Monitor.Wait(CrossWriteBuffer);
					}
				} while (temp > 0);
			}
			SetReachedEndStatus(true);
		}

		public void WriteDecompressed(string destinationPath) {
			using (var fsOutput = new FileStream(destinationPath, FileMode.Create, FileAccess.Write)) {
				while (!ReachedEnd || CrossWriteBuffer.Count != 0) {
					lock (CrossWriteBuffer) {
						var localBuffer = DequeueBuffer();
						fsOutput.Write(localBuffer, 0, localBuffer.Length);
						Monitor.Pulse(CrossWriteBuffer);
						if (!ReachedEnd) Monitor.Wait(CrossWriteBuffer);
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
		static void EnqueueBuffer(byte[] buffer) {
			CrossWriteBuffer.Enqueue(buffer);
		}
		[MethodImplAttribute(MethodImplOptions.Synchronized)]
		static byte[] DequeueBuffer() {
			return CrossWriteBuffer.Dequeue();
		}

		/*
		 * Устанавливает значение reachedEnd в сигнальное
		 * состояние. Используется для подачи сигнала
		 * потокам об окончании чтения из файла.
		 */
		static void SetReachedEndStatus(bool setStatus) {
			lock (CrossWriteBuffer) {
				ReachedEnd = setStatus;
				Monitor.Pulse(CrossWriteBuffer);	//подает последний сигнал пишущему потоку
			}
		}
	}
				
	/*
	 * Класс, для конвертирования в однопроцессорной
	 * среде.
	 */
    class DefaultGZip
    {
        public static void DoCompress(string fileSource, string fileDestination) {
            using (var fsInput = new FileStream(fileSource, FileMode.Open, FileAccess.Read))
                using (var fsOutput = new FileStream(fileDestination, FileMode.Create, FileAccess.Write))
                    using (var gzipStream = new GZipStream(fsOutput, CompressionMode.Compress)) {
                        var buffer = new byte[Constants.BufferSize];
						int temp;
						while ((temp = fsInput.Read(buffer, 0, buffer.Length)) > 0) {
							gzipStream.Write(buffer, 0, temp);
                        }
                    }
        }

		public static void DoDecompress(string fileSource, string fileDestination) {
			using (var fsInput = new FileStream(fileSource, FileMode.Open, FileAccess.Read)) 
				using (var fsOutput = new FileStream(fileDestination, FileMode.Create, FileAccess.Write)) 
					using (var gzipStream = new GZipStream(fsInput, CompressionMode.Decompress)) {
						var buffer = new byte[Constants.BufferSize];
						int temp;
						while ((temp = gzipStream.Read(buffer, 0, buffer.Length)) > 0) {
							fsOutput.Write(buffer, 0, temp);
						}
					}
		}
    }
}