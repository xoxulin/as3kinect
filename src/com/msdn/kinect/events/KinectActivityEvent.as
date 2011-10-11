package com.msdn.kinect.events
{
	import flash.events.Event;
	
	public class KinectActivityEvent extends Event
	{
		public static const ACTIVITY_CHANGE:String = "activityChange";
		
		public var connected:Boolean;
		
		public function KinectActivityEvent(connected:Boolean)
		{
			super(ACTIVITY_CHANGE);
			
			this.connected = connected;
		}
	}
}