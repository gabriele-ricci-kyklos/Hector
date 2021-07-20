using Hector.Core.Support;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Hector.Core.Pdf
{
    public class PdfHelper
    {
        private class KeyedStreamWithValue<T>
        {
            public Guid Key { get; private set; }
            public Stream Stream;
            public T Value { get; private set; }

            public KeyedStreamWithValue(Stream stream, T value)
            {
                stream.AssertNotNull("stream");

                Key = Guid.NewGuid();
                Stream = stream;
                Value = value;
            }
        }

        private PdfDocument MergePDFFilesImpl<T>(IList<KeyedStreamWithValue<T>> streamList, out IList<Exception> errorList, out IList<KeyedStreamWithValue<T>> filesInError)
        {
            streamList.AssertNotNullAndHasElements("files");

            using (PdfDocument outputDocument = new PdfDocument())
            {
                outputDocument.PageLayout = PdfPageLayout.TwoColumnLeft;
                errorList = new List<Exception>();
                filesInError = new List<KeyedStreamWithValue<T>>();

                foreach (KeyedStreamWithValue<T> stream in streamList)
                {
                    IList<PdfPage> singleDocPageList = new List<PdfPage>();

                    try
                    {
                        using (PdfDocument inputDocument = PdfReader.Open(stream.Stream, PdfDocumentOpenMode.Import))
                        {
                            for (int i = 0; i < inputDocument.PageCount; ++i)
                            {
                                PdfPage page = inputDocument.Pages[i];
                                singleDocPageList.Add(page);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        errorList.Add(ex);
                        filesInError.Add(stream);
                        singleDocPageList = new List<PdfPage>();
                    }
                    finally
                    {
                        for (int i = 0; i < singleDocPageList.Count; ++i)
                        {
                            outputDocument.AddPage(singleDocPageList[i]);
                        }
                    }
                }

                return outputDocument;
            }
        }

        public static void TryMergePDFFiles(IEnumerable<byte[]> fileList, string destFilePath, out IList<Exception> errorList, out IList<byte[]> filesInError)
        {
            fileList.AssertNotNullAndHasElementsNotNull("fileList");
            destFilePath.AssertHasText("destFilePath");

            if (fileList.Count() == 1)
            {
                errorList = new List<Exception>();
                filesInError = new List<byte[]>();
                File.WriteAllBytes(destFilePath, fileList.First());
                return;
            }

            var streamList =
                fileList
                    .Select(x => new KeyedStreamWithValue<byte[]>(new MemoryStream(x), x))
                    .ToList();

            PdfHelper helper = new PdfHelper();
            errorList = new List<Exception>();
            IList<KeyedStreamWithValue<byte[]>> streamsInError = new List<KeyedStreamWithValue<byte[]>>();

            using (PdfDocument document = helper.MergePDFFilesImpl(streamList, out errorList, out streamsInError))
            {
                filesInError =
                    streamsInError
                        .Join
                        (
                            streamList,
                            x => x.Key,
                            x => x.Key,
                            (x, y) => y.Value
                        )
                        .ToList();

                document.Save(destFilePath);
            }
        }

        public static byte[] TryMergePDFFiles(IEnumerable<byte[]> fileList, out IList<Exception> errorList, out IList<byte[]> filesInError)
        {
            fileList.AssertNotNullAndHasElementsNotNull("fileList");

            if (fileList.Count() == 1)
            {
                errorList = new List<Exception>();
                filesInError = new List<byte[]>();
                return fileList.First();
            }

            var streamList =
                fileList
                    .Select(x => new KeyedStreamWithValue<byte[]>(new MemoryStream(x), x))
                    .ToList();

            PdfHelper helper = new PdfHelper();
            errorList = new List<Exception>();
            IList<KeyedStreamWithValue<byte[]>> streamsInError = new List<KeyedStreamWithValue<byte[]>>();

            using (PdfDocument document = helper.MergePDFFilesImpl(streamList, out errorList, out streamsInError))
            {
                using (MemoryStream stream = new MemoryStream())
                {
                    filesInError =
                        streamsInError
                            .Join
                            (
                                streamList,
                                x => x.Key,
                                x => x.Key,
                                (x, y) => y.Value
                            )
                            .ToList();

                    document.Save(stream);
                    return stream.ToArray();
                }
            }
        }

        public static void TryMergePDFFiles(IEnumerable<string> filePathList, string destFilePath, out IList<Exception> errorList, out IList<string> filesInError)
        {
            filePathList.AssertNotNullAndHasElementsNotNull("filePathList");
            destFilePath.AssertHasText("destFilePath");

            if (filePathList.Count() == 1)
            {
                errorList = new List<Exception>();
                filesInError = new List<string>();
                File.Copy(filePathList.First(), destFilePath);
                return;
            }

            var streamList =
                filePathList
                    .Select(x => new KeyedStreamWithValue<string>(new FileStream(x, FileMode.Open, FileAccess.Read), x))
                    .ToList();

            PdfHelper helper = new PdfHelper();
            errorList = new List<Exception>();
            IList<KeyedStreamWithValue<string>> streamsInError = new List<KeyedStreamWithValue<string>>();

            using (PdfDocument document = helper.MergePDFFilesImpl(streamList, out errorList, out streamsInError))
            {
                filesInError =
                    streamsInError
                        .Join
                        (
                            streamList,
                            x => x.Key,
                            x => x.Key,
                            (x, y) => y.Value
                        )
                        .ToList();

                document.Save(destFilePath);
            }
        }

        public static byte[] TryMergePDFFiles(IEnumerable<string> filePathList, out IList<Exception> errorList, out IList<string> filesInError)
        {
            filePathList.AssertNotNullAndHasElementsNotNull("filePathList");

            if (filePathList.Count() == 1)
            {
                errorList = new List<Exception>();
                filesInError = new List<string>();
                return File.ReadAllBytes(filePathList.First());
            }

            var streamList =
                filePathList
                    .Select(x => new KeyedStreamWithValue<string>(new FileStream(x, FileMode.Open, FileAccess.Read), x))
                    .ToList();

            PdfHelper helper = new PdfHelper();
            errorList = new List<Exception>();
            IList<KeyedStreamWithValue<string>> streamsInError = new List<KeyedStreamWithValue<string>>();

            using (PdfDocument document = helper.MergePDFFilesImpl(streamList, out errorList, out streamsInError))
            {
                using (MemoryStream stream = new MemoryStream())
                {
                    filesInError =
                        streamsInError
                            .Join
                            (
                                streamList,
                                x => x.Key,
                                x => x.Key,
                                (x, y) => y.Value
                            )
                            .ToList();

                    document.Save(stream);
                    return stream.ToArray();
                }
            }
        }

        public static void MergePDFFiles(IEnumerable<byte[]> fileList, string destFilePath)
        {
            TryMergePDFFiles(fileList, destFilePath, out IList<Exception> errorList, out IList<byte[]> filesInError);

            if (errorList.Any())
            {
                throw new AggregateException("One or more errors occurred in merging PDF files", errorList);
            }
        }

        public static byte[] MergePDFFiles(IEnumerable<byte[]> fileList)
        {
            var bytes = TryMergePDFFiles(fileList, out IList<Exception> errorList, out IList<byte[]> filesInError);

            if (errorList.Any())
            {
                throw new AggregateException("One or more errors occurred in merging PDF files", errorList);
            }

            return bytes;
        }

        public static void MergePDFFiles(IEnumerable<string> filePathList, string destFilePath)
        {
            TryMergePDFFiles(filePathList, destFilePath, out IList<Exception> errorList, out IList<string> filesInError);

            if (errorList.Any())
            {
                throw new AggregateException("One or more errors occurred in merging PDF files", errorList);
            }
        }

        public static byte[] MergePDFFiles(IEnumerable<string> filePathList)
        {
            var bytes = TryMergePDFFiles(filePathList, out IList<Exception> errorList, out IList<string> filesInError);

            if (errorList.Any())
            {
                throw new AggregateException("One or more errors occurred in merging PDF files", errorList);
            }

            return bytes;
        }
    }
}
