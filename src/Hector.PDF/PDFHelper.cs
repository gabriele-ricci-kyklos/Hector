﻿using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Hector.PDF
{
    public static class PDFMergeHelper
    {
        #region Merge

        public static PdfDocument MergePDFFiles(Stream[] streamList)
        {
            using PdfDocument outputDocument = new();
            outputDocument.PageLayout = PdfPageLayout.TwoColumnLeft;
            List<PdfPage> pageList = [];

            foreach (Stream stream in streamList)
            {
                using PdfDocument inputDocument = PdfReader.Open(stream, PdfDocumentOpenMode.Import);
                foreach (PdfPage page in inputDocument.Pages)
                {
                    outputDocument.AddPage(page);
                }
            }

            return outputDocument;
        }

        public static PdfDocument MergePDFFiles(string[] filePathList)
        {
            FileStream[] streams = [];

            try
            {
                streams =
                    filePathList
                        .Select(x => new FileStream(x, FileMode.Open, FileAccess.Read))
                        .ToArray();

                return MergePDFFiles(streams);
            }
            finally
            {
                Array.ForEach(streams, x => x.Dispose());
            }
        }

        public static PdfDocument MergePDFFiles(byte[][] fileContentList)
        {
            MemoryStream[] streams = [];

            try
            {
                streams =
                    fileContentList
                        .Select(x => new MemoryStream(x))
                        .ToArray();

                return MergePDFFiles(streams);
            }
            finally
            {
                Array.ForEach(streams, x => x.Dispose());
            }
        }

        public static void MergePDFFiles(Stream[] streamList, string destFilePath)
        {
            using PdfDocument doc = MergePDFFiles(streamList);
            doc.Save(destFilePath);
        }

        public static void MergePDFFiles(string[] filePathList, string destFilePath)
        {
            using PdfDocument doc = MergePDFFiles(filePathList);
            doc.Save(destFilePath);
        }

        public static void MergePDFFiles(byte[][] fileContentList, string destFilePath)
        {
            using PdfDocument doc = MergePDFFiles(fileContentList);
            doc.Save(destFilePath);
        }

        public static void MergePDFFiles(Stream[] streamList, Stream destStream)
        {
            using PdfDocument doc = MergePDFFiles(streamList);
            doc.Save(destStream);
        }

        public static void MergePDFFiles(string[] filePathList, Stream destStream)
        {
            using PdfDocument doc = MergePDFFiles(filePathList);
            doc.Save(destStream);
        }

        public static void MergePDFFiles(byte[][] fileContentList, Stream destStream)
        {
            using PdfDocument doc = MergePDFFiles(fileContentList);
            doc.Save(destStream);
        }

        #endregion

        private static PdfDocument RegeneratePDFImpl(PdfDocument inputDocument)
        {
            PdfDocument outputDocument = new();
            foreach (PdfPage page in inputDocument.Pages)
            {
                outputDocument.AddPage(page);
            }
            return outputDocument;
        }

        public static void RegeneratePDF(Stream pdfStream, Stream outputDocumentStream)
        {
            using PdfDocument inputDocument = PdfReader.Open(pdfStream, PdfDocumentOpenMode.Import);
            using PdfDocument outputDocument = RegeneratePDFImpl(inputDocument);
            outputDocument.Save(outputDocumentStream);
        }

        public static byte[] RegeneratePDF(Stream pdfStream)
        {
            using MemoryStream outputDocumentStream = new();
            RegeneratePDF(pdfStream, outputDocumentStream);
            return outputDocumentStream.ToArray();
        }
    }
}
