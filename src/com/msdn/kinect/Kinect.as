package com.msdn.kinect {
	 
 	import flash.utils.ByteArray;
	
	public class Kinect {

		public static const SUCCESS:int = 0;
		public static const ERROR:int = -1;

		public static const SERVER_IP:String = "127.0.0.1";
		public static const SOCKET_PORT:int = 6001;

		public static const MS_ID:int			= 0x80;
		
		public static const IN_VIDEO:int		= 0x00;		
		public static const IN_DEPTH:int		= 0x01;
		public static const IN_SKELETON:int	= 0x02;
		public static const IN_AUDIO:int		= 0x03;
		public static const IN_SPEECH:int	= 0x04;
		
		public static const OUT_VIDEO:int	= 0x80;		
		public static const OUT_DEPTH:int	= 0x81;
		public static const OUT_ADD_WORD:int	= 0x82;
		
		public static const IMG_WIDTH:int = 640;
		public static const IMG_HEIGHT:int = 480;
		
		public static const MINIMUM_PACKET_SIZE:int = 6; // first byte + second byte + length (4 byte)		
	}
}
