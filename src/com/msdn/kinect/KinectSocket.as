package com.msdn.kinect {
	 
	import com.msdn.kinect.events.KinectSocketEvent;
	
	import flash.events.Event;
	import flash.events.EventDispatcher;
	import flash.events.IOErrorEvent;
	import flash.events.ProgressEvent;
	import flash.net.Socket;
	import flash.utils.ByteArray;
	import flash.utils.Endian;
	
	import mx.core.Singleton;

	internal class KinectSocket extends EventDispatcher
	{
		private var _firstByte:uint;
		private var _secondByte:uint;
		private var _packetSize:uint;
		private var _socket:Socket;
		private var _port:Number;

		public function KinectSocket()
		{		
			_socket = new Socket();						
			
			_socket.addEventListener(ProgressEvent.SOCKET_DATA, onSocketData);
			_socket.addEventListener(IOErrorEvent.IO_ERROR, onSocketError);
			_socket.addEventListener(Event.CONNECT, onSocketConnect);
			_socket.addEventListener(Event.CLOSE, onSocketClose);
			
			_socket.endian = Endian.LITTLE_ENDIAN; 
		}
		
		public function connect(host:String = 'localhost', port:uint = 6001):void
		{
			_port = port;
			
			if (!this.connected)
			{
				_socket.connect(host, port);
			}
			else
			{
				dispatchEvent(new KinectSocketEvent(KinectSocketEvent.ONCONNECT, null));
			}
		}
		
		public function get connected():Boolean
		{
			return _socket.connected;
		}
		
		public function close():void
		{
			if (this.connected) _socket.close();
		}
		
		public function sendCommand(data:ByteArray):int
		{
			if(data.length >= Kinect.MINIMUM_PACKET_SIZE)
			{
				_socket.writeBytes(data);
				_socket.flush();
				return Kinect.SUCCESS;
			}
			else
			{
				throw new Error( 'Incorrect data size (' + data.length + '). Expected: ' + Kinect.MINIMUM_PACKET_SIZE);
				return Kinect.ERROR;
			}
		}
		
		private function onSocketData(event:ProgressEvent):void
		{
			while (_socket.bytesAvailable > 0)
			{
				if(_socket.bytesAvailable > 6 && _packetSize == 0)
				{
					_firstByte = _socket.readUnsignedByte();
					_secondByte = _socket.readUnsignedByte();
					_packetSize = _socket.readUnsignedInt();					
				}
				
				if(_packetSize != 0 && _socket.bytesAvailable >= _packetSize)
				{
					var buffer:ByteArray = new ByteArray();
					buffer.endian = Endian.LITTLE_ENDIAN;
					
					_socket.readBytes(buffer, 0, _packetSize);
					
					buffer.position = 0;
					
					var dataObject:Object = {};
					
					dataObject.first = _firstByte;
					dataObject.second = _secondByte;
					dataObject.buffer = buffer;
					
					dispatchEvent(new KinectSocketEvent(KinectSocketEvent.ONDATA, dataObject));
					buffer.clear();
					_packetSize = 0;
				}
				else
				{
					return;
				}
			}			
		}
		
		private function onSocketError(event:IOErrorEvent):void
		{
			dispatchEvent(new KinectSocketEvent(KinectSocketEvent.ONERROR, null));
		}
		
		private function onSocketConnect(event:Event):void
		{
			dispatchEvent(new KinectSocketEvent(KinectSocketEvent.ONCONNECT, null));
		}
		
		private function onSocketClose(event:Event):void
		{
			dispatchEvent(new KinectSocketEvent(KinectSocketEvent.ONCLOSE, null));
		}		
	}
}