package com.msdn.kinect {
	
	import com.msdn.kinect.events.KinectActivityEvent;
	import com.msdn.kinect.events.KinectSkeletonEvent;
	import com.msdn.kinect.events.KinectSocketEvent;
	import com.msdn.kinect.events.KinectSpeechEvent;
	
	import flash.events.EventDispatcher;
	import flash.utils.ByteArray;
	import flash.utils.Endian;
	import flash.utils.setInterval;
	import flash.utils.setTimeout;
	
	[Event(name="activityChange", type="com.msdn.kinect.events.KinectActivityEvent")]
	
	public class KinectWrapper extends EventDispatcher {

		private static var _instance:KinectWrapper;
		
		private var _socket:KinectSocket;
		private var _connecting:Boolean = false;
		
		private var _data:ByteArray;
		private var _debugging:Boolean = true;
		private var user_id:Number;
		
		public function KinectWrapper(singleton:Singleton)
		{
			/* Init socket objects */
			_socket = new KinectSocket();
			
			_socket.addEventListener(KinectSocketEvent.ONDATA, dataReceived);
			_socket.addEventListener(KinectSocketEvent.ONCONNECT, connectedHandler);
			_socket.addEventListener(KinectSocketEvent.ONCLOSE, closeHandler);
			_socket.addEventListener(KinectSocketEvent.ONERROR, errorHandler);
			
			setInterval(checkSocket, 10000);
			connect();			
			
			/* Init data out buffer */
			_data = new ByteArray();
			_data.endian = Endian.LITTLE_ENDIAN;
		}
		
		private function connect():void
		{
			_connecting = true;
			_socket.connect(Kinect.SERVER_IP, Kinect.SOCKET_PORT);
		}
		
		private function checkSocket():void
		{
			//trace("_socket.connected", _socket.connected);
		}
		
		private function connectedHandler(event:KinectSocketEvent):void
		{
			//trace("connectedHandler");
			_connecting = false;
			dispatchEvent(new KinectActivityEvent(_socket.connected));
		}
		
		private function closeHandler(event:KinectSocketEvent):void
		{
			//trace("closeHandler");
			dispatchEvent(new KinectActivityEvent(_socket.connected));
			setTimeout(connect, 5000);
		}
		
		private function errorHandler(event:KinectSocketEvent):void
		{
			//trace("_socket.error");
			_connecting = false;
			_socket.close();
			setTimeout(connect, 5000);
		}

		private function dataReceived(event:KinectSocketEvent):void
		{
			// Send ByteArray to position 0
			
			event.data.buffer.position = 0;
			
			switch (event.data.first)
			{
				case Kinect.MS_ID:
				{
					switch (event.data.second)
					{
						case Kinect.IN_VIDEO:
						break;
						case Kinect.IN_DEPTH:
						break;
						case Kinect.IN_SKELETON:
						{
							dispatchEvent(new KinectSkeletonEvent(KinectSkeletonEvent.SKELETON_UPDATE, event.data.buffer));
						}
						break;
						case Kinect.IN_AUDIO:
						break;
						case Kinect.IN_SPEECH:
						{
							dispatchEvent(new KinectSpeechEvent(KinectSpeechEvent.SPEECH_RECOGNIZED, event.data.buffer));
						}
						break;
					}
				}
				break;
			}
			
			// Clear ByteArray after used
			event.data.buffer.clear();
		}
		
		public function get connected():Boolean
		{
			return _socket.connected;
		}
		
		public function addWords(words:Array):void
		{
			if (_socket.connected)
			{
				_data.clear();
				
				var packetSize:uint = 0;
				
				words.forEach(function(word:String, index:uint, array:Array):void {
					packetSize += 2 + word.length; // 2byte length + word
				});
				
				_data.writeByte(Kinect.MS_ID);
				_data.writeByte(Kinect.OUT_ADD_WORD);
				_data.writeUnsignedInt(packetSize);
				
				words.forEach(function(word:String, index:uint, array:Array):void {
					_data.writeUTF(word);
				});
				
				if(_socket.sendCommand(_data) != Kinect.SUCCESS){
					throw new Error('Data was not complete');
				}
			}
		}
		
		public function close():void
		{
			if (_socket.connected) _socket.close();
		}
		
		// - - - instance
		
		public static function get instance():KinectWrapper 
		{
			if ( _instance == null )
			{
				_instance = new KinectWrapper(new Singleton());
			}
			return _instance;
		}
	}	
}

class Singleton {}