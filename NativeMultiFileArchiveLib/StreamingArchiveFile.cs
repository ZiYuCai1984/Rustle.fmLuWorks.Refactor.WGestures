using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;

#nullable disable


namespace NativeMultiFileArchiveLib
{
    /// <summary>
    ///     Implements an archive file format that can be partially read. Thus the stored archive can
    ///     be larger than the available ram, also the system doesn't waste resources processing
    ///     files until they are required to be extracted.
    ///     Limitation is that an individual file must fit into RAM.
    ///     the file is built in blocks:
    ///     each block, starts with 2 bytes indicating the length of the index.
    ///     the next set of bytes is the index.
    ///     the index data stores the filename and length of the following chunk of data.
    ///     the index data is stored by serializing a RawIndex object.
    ///     the file data is stored by serializing and compressing an ArchiveFile object.
    ///     all the indexes can be read in one pass, the indexes give enough data that the reader can
    ///     skip over the data block to read the next index.
    /// </summary>
    public class StreamingArchiveFile
    {
        #region Fields

        /// <summary>
        ///     handles the file indexes.
        /// </summary>
        private IndexHandler _index;

        /// <summary>
        ///     the full path to the file holding the data.
        /// </summary>
        private string _fileName;

        #endregion

        #region Constructor

        /// <summary>
        ///     creates a new streaming archive file with the given list of files.
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="fileData"></param>
        public StreamingArchiveFile(string fileName, List<ArchiveFile> fileData)
        {
            // record the archive file name
            _fileName = fileName;

            // create a new index handler:
            _index = new IndexHandler();

            // write the file data to the stream
            using var s = this.GetStream();
            AddData(fileData, s, _index);
        }

        /// <summary>
        ///     opens an existing streaming archive file and reads in the indexes.
        ///     if the file doesn't exist, or is zero length, prepares to write to the file.
        /// </summary>
        /// <param name="fileName"></param>
        public StreamingArchiveFile(string fileName)
        {
            if (File.Exists(fileName))
            {
                this.OpenArchive(fileName);
            }
            else
            {
                // just be ready to add files:
                _fileName = fileName;
                _index = new IndexHandler();
            }
        }

        #endregion

        #region Properties

        /// <summary>
        ///     returns a reference to the index handler.
        /// </summary>
        public IndexHandler FileIndex => _index;

        /// <summary>
        ///     returns the archive file name.
        /// </summary>
        public string ArchiveFileName => _fileName;

        #endregion

        #region Archive Operations

        /// <summary>
        ///     open an existing streaming archive.
        /// </summary>
        /// <param name="fileName"></param>
        public void OpenArchive(string fileName)
        {
            // record the filename:
            _fileName = fileName;

            // create a new index handler:
            _index = new IndexHandler();

            // read the index data:
            using var fs = File.OpenRead(fileName);
            // read all the indexes from the filestream
            _index.ReadIndexes(fs);
        }

        /// <summary>
        ///     remove a list of files from the archive by the archive file index.
        ///     this works by duplicating the archive file into a temp location, skipping the files due to be
        ///     deleted
        ///     and then copying the duplicate back over the original.
        ///     not the most inspired solution but better than nothing.
        /// </summary>
        /// <param name="filesToRemove"></param>
        public void RemoveFiles(List<ArchiveFileIndex> filesToRemove)
        {
            // create a temporary file:
            var tempFileName = Path.GetTempFileName();

            // create a temporary archive:
            var tempArc = new StreamingArchiveFile(tempFileName);

            // extract each file from this archive, add it to the other archive,
            // except the files to be deleted.
            using (var input = this.GetStream())
            {
                using (var output = tempArc.GetStream())
                {
                    // enumerate the indexes:
                    foreach (var idx in _index.ArchiveIndex)
                    {
                        if (!filesToRemove.Contains(idx))
                        {
                            // process this... read in the raw data block:
                            input.Seek(idx.FileStartIndex, SeekOrigin.Begin);

                            // read the compressed file data:
                            var data = IndexHandler.ReadBuffer(input, idx.FileLength);

                            // now generate the raw index:
                            var rawIndex = new RawIndex
                            {
                                N = idx.FileName,
                                L = idx.FileLength
                            };

                            // add to the destination:
                            AddRawData(output, rawIndex, data, tempArc._index);
                        }
                    }
                }
            }


            // now copy the temp file back over the archive:
            File.Delete(_fileName);
            File.Move(tempFileName, _fileName);

            // now re-read the indexes:
            this.OpenArchive(_fileName);
        }

        /// <summary>
        ///     extracts a file from the archive, decompresses it and returns the file information.
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public ArchiveFile GetFile(string fileName)
        {
            // get the index file:
            var index = _index[fileName];

            // return the archive file object
            return this.GetFile(index);
        }

        /// <summary>
        ///     extracts a file from the archive using the index.
        /// </summary>
        /// <param name="fileIndex"></param>
        /// <returns></returns>
        public ArchiveFile GetFile(ArchiveFileIndex fileIndex)
        {
            using var s = this.GetStream();
            return fileIndex.ExtractFile(s);
        }

        /// <summary>
        ///     adds a new file to the end of the archive.
        /// </summary>
        /// <param name="file"></param>
        public void AddFile(ArchiveFile file)
        {
            // add the specified file to this archive.
            var fileList = new List<ArchiveFile>
            {
                file
            };

            using var s = this.GetStream();
            AddData(fileList, s, _index);
        }

        /// <summary>
        ///     adds a block of files to the archive.
        /// </summary>
        /// <param name="filesToAdd"></param>
        public void AddFiles(List<ArchiveFile> filesToAdd)
        {
            using var s = this.GetStream();
            AddData(filesToAdd, s, _index);
        }

        /// <summary>
        ///     opens a stream used to read or write the archive file.
        /// </summary>
        /// <returns></returns>
        public Stream GetStream()
        {
            return new FileStream(_fileName, FileMode.OpenOrCreate, FileAccess.ReadWrite);
        }

        #endregion

        #region Static

        /// <summary>
        ///     creates an archive with the given array of files, and returns the index handler.
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public static IndexHandler CreateArchiveFile(string fileName, List<ArchiveFile> data)
        {
            var handler = new IndexHandler();
            using var fs = File.Create(fileName);
            AddData(data, fs, handler);

            return handler;
        }

        /// <summary>
        ///     adds a list of files to an archive.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="destStream"></param>
        /// <param name="updIndex"></param>
        public static void AddData(List<ArchiveFile> data, Stream destStream, IndexHandler updIndex)
        {
            if (!destStream.CanWrite)
            {
                throw new ArgumentException("Cannot Write to Stream");
            }

            if (!destStream.CanSeek)
            {
                throw new ArgumentException("Cannot Seek!");
            }

            // enumerate the archive files: generate the index and the data.
            foreach (var file in data)
            {
                // serialize and compress the archive file
                var serialData = file.ToByteArray();

                // generate a new index:
                var idx = new RawIndex
                {
                    N = file.ToString(),
                    L = serialData.Length
                };
                AddRawData(destStream, idx, serialData, updIndex);
            }
        }

        /// <summary>
        ///     writes the index data and the raw file blocks to the destination stream.
        /// </summary>
        /// <param name="destStream"></param>
        /// <param name="idx"></param>
        /// <param name="dataArray"></param>
        /// <param name="updIndex"></param>
        public static void AddRawData(
            Stream destStream, RawIndex idx, byte[] dataArray, IndexHandler updIndex)
        {
            // serialize and compress the index data:
            var indexData = RawIndex.ToByteArray(idx);

            // generate the header bytes:
            var header = BitConverter.GetBytes((ushort) indexData.Length);

            // move to the end of the stream:
            destStream.Position = destStream.Length;

            // write the header data:
            destStream.Write(header, 0, header.Length);

            // write the index data:
            destStream.Write(indexData, 0, indexData.Length);

            // add the index record to the handler:
            updIndex.ArchiveIndex.Add(
                new ArchiveFileIndex
                {
                    FileLength = idx.L,
                    FileName = idx.N,
                    FileStartIndex = destStream.Position
                });

            // write the data to the stream
            destStream.Write(dataArray, 0, dataArray.Length);
        }

        #endregion
    }

    /// <summary>
    ///     handles the indexes in a streaming archive.
    /// </summary>
    public class IndexHandler
    {
        #region Fields

        /// <summary>
        ///     internal list of archive file indexes.
        ///     these specify where a file is within the archive.
        /// </summary>
        private readonly List<ArchiveFileIndex> _fileIndex = new List<ArchiveFileIndex>();

        #endregion

        /// <summary>
        ///     locates the files in the given directory.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public IEnumerable<ArchiveFileIndex> GetDirectoryFiles(string path)
        {
            return from file in _fileIndex
                where Path.GetDirectoryName(file.FileName)!
                    .Equals(path, StringComparison.OrdinalIgnoreCase)
                select file;
        }

        /// <summary>
        ///     build a list of the unique directories in the archive index.
        /// </summary>
        /// <returns></returns>
        public List<string> GetDirectories()
        {
            var dirs = new List<string>();
            foreach (var file in this.IndexedFileNames)
            {
                var path = Path.GetDirectoryName(file);
                if (!dirs.Contains(path))
                {
                    dirs.Add(path);
                }
            }

            return dirs;
        }

        /// <summary>
        ///     populate a tree-view control with the folder structure inside the archive.
        /// </summary>
        /// <param name="tv"></param>
        public void PopulateFolderTreeView(TreeView tv)
        {
            var pathSep = '\\';
            tv.PathSeparator = pathSep.ToString();
            tv.Nodes.Clear();

            // maintain a dictionary of paths and nodes.
            var nodes = new Dictionary<string, TreeNode>();

            foreach (var dir in this.GetDirectories())
            {
                var pathElements = dir.Split(pathSep);
                var path = "";

                TreeNode parent = null;

                foreach (var pathElement in pathElements)
                {
                    if (path.Length > 0)
                    {
                        path += pathSep;
                    }

                    path += pathElement.ToUpper();

                    if (parent != null)
                    {
                        if (nodes.ContainsKey(path))
                        {
                            parent = nodes[path];
                        }
                        else
                        {
                            parent = parent.Nodes.Add(path, pathElement);
                            nodes.Add(path, parent);
                        }
                    }
                    else
                    {
                        if (nodes.ContainsKey(path))
                        {
                            parent = nodes[path];
                        }
                        else
                        {
                            parent = tv.Nodes.Add(path, pathElement);
                            nodes.Add(path, parent);
                        }
                    }
                }
            }
        }

        /// <summary>
        ///     search the index for a file matching the expression.
        /// </summary>
        /// <param name="fileNameExpression"></param>
        /// <returns></returns>
        public IEnumerable<ArchiveFileIndex> Search(string fileNameExpression)
        {
            return from file in _fileIndex
                where Regex.IsMatch(file.FileName, fileNameExpression)
                select file;
        }

        /// <summary>
        ///     reads in all the indexes from the specified input stream.
        /// </summary>
        /// <param name="input"></param>
        public void ReadIndexes(Stream input)
        {
            // reads all the index data from the filestream
            while (input.Position < input.Length)
            {
                // get the starting position of the index block.
                var indexStartPosition = input.Position;

                // read in the raw index descriptor:
                var raw = ReadNextIndexBlock(input, out var indexLength);

                // add a new index to the list:
                _fileIndex.Add(
                    new ArchiveFileIndex
                    {
                        // keep the length, name, start index, and index block position/length
                        FileLength = raw.L,
                        FileName = raw.N,
                        FileStartIndex = input.Position,
                        IndexBlockPositionStart = indexStartPosition,
                        IndexBlockLength = indexLength
                    });

                // skip the appropriate amount of the file:
                input.Position += raw.L;
            }
        }

        #region Properties

        /// <summary>
        ///     gets the list of archive file indexes.
        /// </summary>
        public List<ArchiveFileIndex> ArchiveIndex => _fileIndex;

        /// <summary>
        ///     get the index for the specified element id.
        /// </summary>
        /// <param name="fileIndex"></param>
        /// <returns></returns>
        public ArchiveFileIndex this[int fileIndex] => _fileIndex[fileIndex];

        /// <summary>
        ///     get the index for the specified file.
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public ArchiveFileIndex this[string fileName]
        {
            get
            {
                // find the index from the filename:
                foreach (var idx in _fileIndex)
                {
                    if (idx.FileName.Equals(fileName, StringComparison.OrdinalIgnoreCase))
                    {
                        return idx;
                    }
                }

                throw new ArgumentException("File Not Found: " + fileName);
            }
        }

        /// <summary>
        ///     get an enumerable of index names
        /// </summary>
        public IEnumerable<string> IndexedFileNames => from file in _fileIndex
            select file.FileName;

        #endregion

        #region Static

        /// <summary>
        ///     extract an archive file from the source stream.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public static ArchiveFile ExtractArchive(Stream source, ArchiveFileIndex index)
        {
            // seek to the start index of the file:
            source.Seek(index.FileStartIndex, SeekOrigin.Begin);

            // read the block of data:
            var archiveData = ReadBuffer(source, index.FileLength);

            // deserialize the file:
            return ArchiveFile.FromByteArray(archiveData);
        }

        /// <summary>
        ///     retrieves the next raw-index from the stream.
        ///     assumes that the position of the filestream is at the start of an index block.
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public static RawIndex ReadNextIndexBlock(Stream stream)
        {
            return ReadNextIndexBlock(stream, out _);
        }

        /// <summary>
        ///     read in the next index descriptor, and pass out the length.
        ///     this assumes that the position of the filestream is at the start of an index.
        /// </summary>
        /// <param name="fs"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public static RawIndex ReadNextIndexBlock(Stream fs, out int length)
        {
            // read in the length of the index data
            length = ReadNextIndexDataLength(fs);

            // read in the block of index data:
            var idxBlock = ReadBuffer(fs, length);

            // deserialize it:
            return RawIndex.FromByteArray(idxBlock);
        }

        /// <summary>
        ///     helper method: reads in the next 2 bytes and converts to an integer;
        /// </summary>
        /// <param name="fs"></param>
        /// <returns></returns>
        private static int ReadNextIndexDataLength(Stream fs)
        {
            // read 4 bytes from the file-stream
            var lengthBuffer = new byte[2];
            fs.Read(lengthBuffer, 0, 2);

            // convert to an Int32
            return BitConverter.ToUInt16(lengthBuffer, 0);
        }

        /// <summary>
        ///     helper method: reads in a block of data from the filestream.
        /// </summary>
        /// <param name="fs"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public static byte[] ReadBuffer(Stream fs, int length)
        {
            var indexBuffer = new byte[length];
            var offset = 0;
            var remaining = indexBuffer.Length;
            while (remaining > 0)
            {
                // read a block into the index buffer:
                var read = fs.Read(indexBuffer, offset, remaining);

                // increment the offset:
                offset += read;

                // decrement the amount remaining.
                remaining -= read;
            }

            return indexBuffer;
        }

        #endregion
    }

    /// <summary>
    ///     represents and individual file index.
    /// </summary>
    public class ArchiveFileIndex
    {
        /// <summary>
        ///     the name of the file.
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        ///     the start index of the file within the archive.
        /// </summary>
        public long FileStartIndex { get; set; }

        /// <summary>
        ///     the compressed length of the file.
        /// </summary>
        public int FileLength { get; set; }

        /// <summary>
        ///     the start location of the index block for this file.
        /// </summary>
        public long IndexBlockPositionStart { get; set; }

        /// <summary>
        ///     the length of the index block for this file.
        /// </summary>
        public int IndexBlockLength { get; set; }

        /// <summary>
        ///     extract this file from the specified stream.
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public ArchiveFile ExtractFile(Stream source)
        {
            return IndexHandler.ExtractArchive(source, this);
        }
    }

    /// <summary>
    ///     index storage class: this is as small as possible.
    ///     the data is never going to be long enough to be worth compressing (the compression will make it
    ///     longer)
    ///     so keep this classes' serialization as tiny as possible.
    /// </summary>
    [Serializable]
    public class RawIndex
    {
        /// <summary>
        ///     the length of the file.
        /// </summary>
        public int L;

        /// <summary>
        ///     the name of the file.
        /// </summary>
        public string N;

        /// <summary>
        ///     compresses an array of RawIndex objects to a byte-array.
        /// </summary>
        /// <param name="indexData"></param>
        /// <returns></returns>
        public static byte[] ToByteArray(RawIndex indexData)
        {
            return TinySerializer.Serialize(indexData, true);
        }

        /// <summary>
        ///     restores an array of RawIndex object from a byte-array.
        /// </summary>
        /// <param name="indexData"></param>
        /// <returns></returns>
        public static RawIndex FromByteArray(byte[] indexData)
        {
            return TinySerializer.DeSerialize<RawIndex>(indexData, true);
        }
    }

    /// <summary>
    ///     just used to test the system.
    /// </summary>
    public class StreamingArchiveTester
    {
        public static void TestCreate()
        {
            // create a streaming archive file from existing files:
            var arc = new StreamingArchiveFile(@"C:\Temp\streamingArchive.arc");

            // now iterate through the files in a specific directory and add them to the archive:
            foreach (var fileName in Directory.GetFiles(@"C:\Temp\Test\", "*.*"))
            {
                // add the file
                arc.AddFile(new ArchiveFile(fileName));
            }
        }

        public static void TestOpen()
        {
            // open the existing archive file:
            var arc = new StreamingArchiveFile(@"C:\Temp\streamingArchive.arc");

            // iterate the files in the archive:
            foreach (var fileName in arc.FileIndex.IndexedFileNames)
            {
                // write the name of the file
                Debug.Print("File: " + fileName);

                // extract the file:
                var file = arc.GetFile(fileName);

                // save it to disk:
                var tempFileName = Path.GetTempPath() + "\\" + file.Name;
                file.SaveAs(tempFileName);

                // open the file:
                Process.Start(tempFileName);
            }
        }
    }
}
