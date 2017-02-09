using System;
using System.IO;
using System.Diagnostics;
using System.Threading;
using System.IO.Compression;
using System.Collections.Generic;

namespace GzipStreamDemo
{
    /*
     * Класс включающий в себя константы 
     */
    public static class Constants {
        public const string Compress = "compress", Decompress = "decompress"; //команды
		public const int BufferSize = 4096;
        public static bool Aborted = false;
        public static int ReturnValue = 0;
     }
    
    /* 
     * Класс для организации работы потоков 
     */
    public class WorkerThread {
        public Thread Thread;

        public WorkerThread(string pathToFile, string pathToZip, int sectorId, bool compressOrDecompressMode) {
            Thread = new Thread(this.RunTask);
            IsCompressMode = compressOrDecompressMode;
            this.SectorId = sectorId;
            this.PathToFile = pathToFile;
			this.PathToZip = pathToZip;
        }

        void RunTask() {
			if (IsCompressMode) {
				if (SectorId == 0) {
					new MultiThreadGZip().DoRead(PathToFile);
				} else {
					Thread.Sleep(50);	//Разрешить первому потоку чтение из файла, перед началом записи.
					new MultiThreadGZip().WriteCompressed(PathToZip);
				}
			} else if (SectorId == 0) {
				new MultiThreadGZip().DoReadCompressed(PathToFile);
			} else {
				Thread.Sleep(50);
				new MultiThreadGZip().WriteDecompressed(PathToZip);
			}
        }

        public int SectorId { get; set; }
        string PathToFile { get; set; }
        string PathToZip { get; set; }
        bool IsCompressMode { get; set; }
    }

    /* 
	 * Здесь аргументы проверяются на корректность
     * после чего допускаются для использования
     */
    public class ThreadProcessClass
    {
		static WorkerThread[] _threads = new WorkerThread[2];
        string[] _args;

		/*
		 * Конструктор
		 */
        public ThreadProcessClass(string[] arguments) {
            _args = arguments;
            try {
                if (IsCompressCommand(_args) || IsDecompressCommand(_args)) {   // если первый аргумент - одна из двух комманд
                    if (IsPathCanBeUsed(_args)) {                              // если может использоваться в качестве указателей директорий
                        Console.CancelKeyPress += (sender, eventArgs) => {
                            eventArgs.Cancel = true;
                            Constants.Aborted = true;
                            foreach (WorkerThread wt in _threads) { wt.Thread.Abort(); }
                        };
						if (Environment.ProcessorCount < 2) {					//если процессор всего один
							var defaultWorkerThread = new Thread(() => {
								if (IsCompressCommand(_args)) DefaultGZip.DoCompress(_args[1], _args[2]);
								else if (IsDecompressCommand(_args)) DefaultGZip.DoDecompress(_args[1], _args[2]);
							});
							defaultWorkerThread.Start();
							defaultWorkerThread.Join();
						} else {
							for (int counter = 0; counter < 2; counter++) {
								_threads[counter] = new WorkerThread(_args[1], _args[2], counter, IsCompressCommand(_args));
							}
							foreach (WorkerThread wt in _threads) { wt.Thread.Start(); }
							foreach (WorkerThread wt in _threads) { wt.Thread.Join(); }
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

        bool IsCompressCommand(string[] args)
        {
            if (Constants.Compress.Equals(args[0])) return true;
            else return false;
        }

        bool IsDecompressCommand(string[] args)
        {
            if (Constants.Decompress.Equals(args[0])) return true;
            else return false;
        }

        /* 
		 * Метод определяющий содержимое args[1] и args[2]
         * в которых должны сожержаться пути к файлам.
         * Также меняет расширения файлов
         */
        bool IsPathCanBeUsed(string[] args)
        {
            if (IsCompressCommand(args) && IsExistedPath(args[1], args[2])) {
                ReplaceExtentionToGz(ref args[2]);
                if (!File.Exists(args[2]))
                    return true;
                else return false;
            } else if (IsDecompressCommand(args) && IsExistedPath(args[1], args[2]) 
                        && Path.GetExtension(args[1]).Equals(".gz")) { return true;
            } else return false;
        }

        /*
         * Проверяет: существует ли файл, существует ли
         * директория и не пустое ли имя выходящего файла,
         * а также не одинаковы ли указанные директории.
         */
        bool IsExistedPath(string path1, string path2) {
            if (File.Exists(path1) && Directory.Exists(Path.GetDirectoryName(path2))
                && Path.GetFileNameWithoutExtension(path2) != "" && _args[1] != _args[2]) return true;
            else return false;
        }

        void ReplaceExtentionToGz(ref string pathToFile) {
            if (Path.GetExtension(pathToFile) != "") pathToFile.Replace(Path.GetExtension(pathToFile), ".gz");
            else pathToFile += ".gz";
        }
        
    }
}
