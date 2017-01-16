﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace P2PNET.ApplicationLayer.EventArgs
{
    public enum TransDirrection
    {
        sending,
        receiving
    }
    public class FileTransferEventArgs : System.EventArgs
    {
        public TransDirrection Dirrection { get; }
        public string FileName { get; }
        public long FileLength { get; }
        public long BytesProcessed { get; }
        public float Percent
        {
            get
            {
                return BytesProcessed / FileLength;
            }
        }

        //constructor
        public FileTransferEventArgs(FileSent fileSend)
        {
            this.FileLength = fileSend.FilePart.TotalFileSizeBytes;
            this.BytesProcessed = fileSend.BytesProcessed;
            this.FileName = fileSend.FilePart.FileName;
            this.Dirrection = TransDirrection.sending;
        }

        //constructor
        public FileTransferEventArgs(FileReceived fileReceived)
        {
            this.FileLength = fileReceived.FilePart.TotalFileSizeBytes;
            this.BytesProcessed = fileReceived.BytesProcessed;
            this.FileName = fileReceived.FilePart.FileName;
            this.Dirrection = TransDirrection.receiving;
        }
    }
}
