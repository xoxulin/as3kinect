package com.msdn.kinect.objects
{
	import flash.utils.ByteArray;

	public class SpeechCommand
	{
		public var text:String;
		public var confidence:Number;
		public var beamAngle:Number;
		
		public function SpeechCommand()
		{
		}
		
		public static function createFromByteArray(byteArray:ByteArray):SpeechCommand
		{
			var speechCommand:SpeechCommand = new SpeechCommand();
			
			var length:int = byteArray.readByte(); // read before byte length of text
			
			speechCommand.text = byteArray.readUTFBytes(length); // than read text
			speechCommand.confidence = byteArray.readFloat();
			speechCommand.beamAngle = byteArray.readFloat();
			
			return speechCommand;
		}
	}
}