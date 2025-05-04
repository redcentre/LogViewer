using System;

namespace RCS.LogViewer;

internal class Busy : IDisposable
{
	public static Busy Show(string message, MainController controller, int? type = null)
	{
		return new Busy(message, controller, type);
	}

	readonly MainController _controller;

	Busy(string message, MainController controller, int? type = null)
	{
		_controller = controller;
		_controller.BusyMessage = message;
		_controller.BusyType = type;
	}

	public void Dispose()
	{
		_controller.BusyMessage = null;
		_controller.BusyType = null;
	}
}
