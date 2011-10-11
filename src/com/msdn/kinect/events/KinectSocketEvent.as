package com.msdn.kinect.events
{
	import flash.events.Event;
	
		public class KinectSocketEvent extends Event
		{
			
		public static const ONCONNECT:String = "ONCONNECT";
		public static const ONCLOSE:String = "ONCLOSE";
		public static const ONDATA:String = "ONDATA";
		public static const ONERROR:String = "ONERROR";
		
		public var data:*;
		
		public function KinectSocketEvent(type:String, data:*)
		{
			this.data = data;
			super(type);
		}
	}
}