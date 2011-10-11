package com.msdn.kinect.events
{
	import com.msdn.kinect.objects.Skeleton;
	
	import flash.events.Event;
	import flash.utils.ByteArray;
	
	public class KinectSkeletonEvent extends Event
	{
		public static const SKELETON_UPDATE:String = "skeletonUpdate";
		
		public var skeleton:Skeleton;
		
		public function KinectSkeletonEvent(type:String, byteArray:ByteArray)
		{
			super(type);
			skeleton = Skeleton.createFromByteArray(byteArray);
		}
	}
}