# GZipStream
### Programm for compress and decompress files (C#)
Created: 14 Feburary 2015
<br><br>
Program for compression/decompression GZip files in means of multithread. You can launch it via console with possible arguments:
<br>
Usage example:
* <b><i>compress [FILE_PATH] [ZIP_PATH]</b></i>
* <b><i>decompress [ZIPPED_FILE_PATH] [RESULT_FILE_PATH]</b></i>


Where: <ul> <li> <b><i>compress/decompres</b></i> are commands; </li>
  <li> <b><i>FILE_PATH</b></i> - path to file that must be compressed;</li>
  <li> <b><i>ZIP_PATH</b></i> - path to store compressed file; </li>
  <li> <b><i>ZIPPED_FILE_PATH</b></i> - path to file, that already been zipped and have *.gz extension; </li>
  <li> <b><i>RESULT_FILE_PATH</b></i> - path to store decompressed file (you must add extension to file name).  </li>
</ul>

This programm works in multicore systems as well as single core systems. 
Check release tab to get latest version!
