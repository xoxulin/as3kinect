using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KinectServer
{
    class KinectConst
    {
        public const byte MS_DATA = 0x80;

        // outgoing data

        public const byte OUT_VIDEO = 0x00;
        public const byte OUT_DEPTH = 0x01;
        public const byte OUT_SKELETON = 0x02;
        public const byte OUT_AUDIO = 0x03;
        public const byte OUT_SPEECH = 0x04;

        // incominging data

        public const byte IN_VIDEO = 0x80;
        public const byte IN_DEPTH = 0x81;
        public const byte IN_ADD_WORD = 0x82;

        public const uint SKELETON_BUFFER_LENGTH = 20 * 3 * 4 + 4 + 4; // 20 joints * 3 coordinates * 4 bytes + 1 id * 4 byte + 4 bytes * 1 uint datestamp

        public const int RED_IDX = 2;
        public const int GREEN_IDX = 1;
        public const int BLUE_IDX = 0;
    }
}
