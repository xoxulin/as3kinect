package com.msdn.kinect.objects
{
	import flash.geom.Vector3D;
	import flash.utils.ByteArray;
		
	public class Skeleton {
		
		public var userId			:	uint;
		
		public var hipCenter		:Vector3D;
		public var spine			:Vector3D;
		public var shoulderCenter	:Vector3D;
		public var head				:Vector3D;
		public var shoulderLeft		:Vector3D;
		public var elbowLeft		:Vector3D;
		public var wristLeft		:Vector3D;
		public var handLeft			:Vector3D;
		public var shoulderRight	:Vector3D;
		public var elbowRight		:Vector3D;
		public var wristRight		:Vector3D;
		public var handRight		:Vector3D;
		public var hipLeft			:Vector3D;
		public var kneeLeft			:Vector3D;
		public var ankleLeft		:Vector3D;
		public var footLeft			:Vector3D;
		public var hipRight			:Vector3D;
		public var kneeRight		:Vector3D;
		public var ankleRight		:Vector3D;
		public var footRight		:Vector3D;
		
		public var time		:uint;
		
		public function Skeleton():void
		{
			this.userId 		= 0;
			this.hipCenter		= new Vector3D();
			this.spine			= new Vector3D();
			this.shoulderCenter	= new Vector3D();
			this.head			= new Vector3D();
			this.shoulderLeft	= new Vector3D();
			this.elbowLeft		= new Vector3D();
			this.wristLeft		= new Vector3D();
			this.handLeft		= new Vector3D();
			this.shoulderRight	= new Vector3D();
			this.elbowRight		= new Vector3D();
			this.wristRight		= new Vector3D();
			this.handRight		= new Vector3D();
			this.hipLeft		= new Vector3D();
			this.kneeLeft		= new Vector3D();
			this.ankleLeft		= new Vector3D();
			this.footLeft		= new Vector3D();
			this.hipRight		= new Vector3D();
			this.kneeRight		= new Vector3D();
			this.ankleRight		= new Vector3D();
			this.footRight		= new Vector3D();
			
			this.time 			= 0;
		}
		
		public static function createFromByteArray(byteArray:ByteArray):Skeleton
		{
			var skeleton:Skeleton = new Skeleton();
			
			skeleton.userId 		= byteArray.readInt();
			skeleton.hipCenter		= new Vector3D(byteArray.readFloat(), byteArray.readFloat(), byteArray.readFloat());
			skeleton.spine			= new Vector3D(byteArray.readFloat(), byteArray.readFloat(), byteArray.readFloat());
			skeleton.shoulderCenter	= new Vector3D(byteArray.readFloat(), byteArray.readFloat(), byteArray.readFloat());
			skeleton.head			= new Vector3D(byteArray.readFloat(), byteArray.readFloat(), byteArray.readFloat());
			skeleton.shoulderLeft	= new Vector3D(byteArray.readFloat(), byteArray.readFloat(), byteArray.readFloat());
			skeleton.elbowLeft		= new Vector3D(byteArray.readFloat(), byteArray.readFloat(), byteArray.readFloat());
			skeleton.wristLeft		= new Vector3D(byteArray.readFloat(), byteArray.readFloat(), byteArray.readFloat());
			skeleton.handLeft		= new Vector3D(byteArray.readFloat(), byteArray.readFloat(), byteArray.readFloat());
			skeleton.shoulderRight	= new Vector3D(byteArray.readFloat(), byteArray.readFloat(), byteArray.readFloat());
			skeleton.elbowRight		= new Vector3D(byteArray.readFloat(), byteArray.readFloat(), byteArray.readFloat());
			skeleton.wristRight		= new Vector3D(byteArray.readFloat(), byteArray.readFloat(), byteArray.readFloat());
			skeleton.handRight		= new Vector3D(byteArray.readFloat(), byteArray.readFloat(), byteArray.readFloat());
			skeleton.hipLeft		= new Vector3D(byteArray.readFloat(), byteArray.readFloat(), byteArray.readFloat());
			skeleton.kneeLeft		= new Vector3D(byteArray.readFloat(), byteArray.readFloat(), byteArray.readFloat());
			skeleton.ankleLeft		= new Vector3D(byteArray.readFloat(), byteArray.readFloat(), byteArray.readFloat());
			skeleton.footLeft		= new Vector3D(byteArray.readFloat(), byteArray.readFloat(), byteArray.readFloat());
			skeleton.hipRight		= new Vector3D(byteArray.readFloat(), byteArray.readFloat(), byteArray.readFloat());
			skeleton.kneeRight		= new Vector3D(byteArray.readFloat(), byteArray.readFloat(), byteArray.readFloat());
			skeleton.ankleRight		= new Vector3D(byteArray.readFloat(), byteArray.readFloat(), byteArray.readFloat());
			skeleton.footRight		= new Vector3D(byteArray.readFloat(), byteArray.readFloat(), byteArray.readFloat());
			
			skeleton.time 		= byteArray.readUnsignedInt();
			
			return skeleton;
		} 
	}
}
