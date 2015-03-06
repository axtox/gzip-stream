using System;
using System.IO;
using System.Diagnostics;
using System.Threading;
using System.IO.Compression;
using System.Collections.Generic;

namespace GZipStream
{
    /*
     * Класс включающий в себя константы 
     */
    public static class Constants {
        public const string compress = "compress", decompress = "decompress"; //команды
		public const int bufferSize = 4096;
        public static bool aborted = false;
        public static int returnValue = 0;
     }
    
    /* 
     * Класс для организации работы потоков 
     */
    public class WorkerThread {
        public Thread workerThread;

        public WorkerThread(string pathToFile, string pathToZip, int sectorID, bool compressOrDecompressMode) {
            workerThread = new Thread(this.runTask);
            isCompressMode = compressOrDecompressMode;
            this.sectorID = sectorID;
            this.pathToFile = pathToFile;
			this.pathToZip = pathToZip;
        }

        void runTask() {
			if (isCompressMode) {
				if (sectorID == 0) {
					new MultiThreadGZip().doRead(pathToFile);
				} else {
					Thread.Sleep(50);	//Разрешить первому потоку чтение из файла, перед началом записи.
					new MultiThreadGZip().writeCompressed(pathToZip);
				}
			} else if (sectorID == 0) {
				new MultiThreadGZip().doReadCompressed(pathToFile);
			} else {
				Thread.Sleep(50);
				new MultiThreadGZip().writeDecompressed(pathToZip);
			}
        }

        public int sectorID { get; set; }
        string pathToFile { get; set; }
        string pathToZip { get; set; }
        bool isCompressMode { get; set; }
    }

    /* 
	 * Здесь аргументы проверяются на корректность
     * после чего допускаются для использования
     */
    public class ThreadProcessClass
    {
		static WorkerThread[] threads = new WorkerThread[2];
        string[] args;

		/*
		 * Конструктор
		 */
        public ThreadProcessClass(string[] arguments) {
            args = arguments;
            try {
                if (isCompressCommand(args) || isDecompressCommand(args)) {   // если первый аргумент - одна из двух комманд
                    if (isPathCanBeUsed(args)) {                              // если может использоваться в качестве указателей директорий
                        Console.CancelKeyPress += (sender, eventArgs) => {
                            eventArgs.Cancel = true;
                            Constants.aborted = true;
                            foreach (WorkerThread wt in threads) { wt.workerThread.Abort(); }
                        };
						if (Environment.ProcessorCount < 2) {					//если процессор всего один
							var defaultWorkerThread = new Thread(() => {
								if (isCompressCommand(args)) DefaultGZip.doCompress(args[1], args[2]);
								else if (isDecompressCommand(args)) DefaultGZip.doDecompress(args[1], args[2]);
							});
							defaultWorkerThread.Start();
							defaultWorkerThread.Join();
						} else {
							for (int counter = 0; counter < 2; counter++) {
								threads[counter] = new WorkerThread(args[1], args[2], counter, isCompressCommand(args));
							}
							foreach (WorkerThread wt in threads) { wt.workerThread.Start(); }
							foreach (WorkerThread wt in threads) { wt.workerThread.Join(); }
						}
                    }
                    else throw new FileNotFoundException("\n\nWrong path or file name. Choose path that already exists.\n"
                                                        + "\nTIPS: \n\t(*) Already compressed file must ends with *.gz"
                                                        + "\n\t(*) Type file name and existed path when creating"
                                                        + "\n\tnew compressed/decopressed file"
                                                        + "\n\t(*) Program won't start if destination file already exists");
                }
                else throw new WrongCommandException("\n\nWrong command. Type \"compress\" or \"decompress\".");
            }
            catch (WrongCommandException wae) { throw wae; }
            catch (FileNotFoundException fnfe) { throw fnfe; }
            catch (InvalidDataException ide) { throw ide; }
        }

        bool isCompressCommand(string[] args)
        {
            if (Constants.compress.Equals(args[0])) return true;
            else return false;
        }

        bool isDecompressCommand(string[] args)
        {
            if (Constants.decompress.Equals(args[0])) return true;
            else return false;
        }

        /* 
		 * Метод определяющий содержимое args[1] и args[2]
         * в которых должны сожержаться пути к файлам.
         * Также меняет расширения файлов
         */
        bool isPathCanBeUsed(string[] args)
        {
            if (isCompressCommand(args) && isExistedPath(args[1], args[2])) {
                replaceExtentionToGZ(ref args[2]);
                if (!File.Exists(args[2]))
                    return true;
                else return false;
            } else if (isDecompressCommand(args) && isExistedPath(args[1], args[2]) 
                        && Path.GetExtension(args[1]).Equals(".gz")) { return true;
            } else return false;
        }

        /*
         * Проверяет: существует ли файл, существует ли
         * директория и не пустое ли имя выходящего файла,
         * а также не одинаковы ли указанные директории.
         */
        bool isExistedPath(string path1, string path2) {
            if (File.Exists(path1) && Directory.Exists(Path.GetDirectoryName(path2))
                && Path.GetFileNameWithoutExtension(path2) != "" && args[1] != args[2]) return true;
            else return false;
        }

        void replaceExtentionToGZ(ref string pathToFile) {
            if (Path.GetExtension(pathToFile) != "") pathToFile.Replace(Path.GetExtension(pathToFile), ".gz");
            else pathToFile += ".gz";
        }
        
    }
}
