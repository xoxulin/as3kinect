package com.msdn.kinect.events
{
	import com.msdn.kinect.objects.Skeleton;
	import com.msdn.kinect.objects.SpeechCommand;
	
	import flash.events.Event;
	import flash.utils.ByteArray;
	
	public class KinectSpeechEvent extends Event
	{
		public static const SPEECH_RECOGNIZED:String = "speechRecognized";
		
		public var speechCommand:SpeechCommand
		
		public function KinectSpeechEvent(type:String, byteArray:ByteArray)
		{
			super(type);
			if (byteArray) speechCommand = SpeechCommand.createFromByteArray(byteArray);		
		}
	}
}