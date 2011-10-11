package com.msdn.kinect
{
	import flash.geom.Vector3D;

	public class KinectProjection
	{
		private static const SEMI_WIDTH:Number = 320;
		private static const SEMI_HEIGHT:Number = 240;
		private static const TG_FOV_2:Number = Math.tan(0.5);
		private static const ADJUSTED_WIDTH:Number = SEMI_WIDTH/TG_FOV_2;
		
		public static function getProjection(vector3D:Vector3D):Vector3D
		{
			var result:Vector3D = new Vector3D();
			
			result.x = SEMI_WIDTH + ADJUSTED_WIDTH * vector3D.x / vector3D.z;
			result.y = SEMI_HEIGHT - ADJUSTED_WIDTH * vector3D.y / vector3D.z;
			result.z = 2 * ADJUSTED_WIDTH * vector3D.z;
			
			return result;
		}
	}
}