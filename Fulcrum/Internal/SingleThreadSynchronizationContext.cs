using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Fulcrum;

internal sealed class SingleThreadSynchronizationContext : SynchronizationContext
{
	private BlockingCollection<KeyValuePair<SendOrPostCallback, object>> m_queue = new BlockingCollection<KeyValuePair<SendOrPostCallback, object>>();

	public override void Post(SendOrPostCallback d, object state)
	{
		m_queue.Add(new KeyValuePair<SendOrPostCallback, object>(d, state));
	}

	public override void Send(SendOrPostCallback d, object state)
	{
		try
		{
			base.Send(d, state);
		}
		catch (Exception e) { GError.RaiseError(e); }
	}

	//public void RunAllOnCurrentThread()
	//{
	//	KeyValuePair<SendOrPostCallback, object> workItem;
	//	while (m_queue.TryTake(out workItem, Timeout.Infinite))
	//		workItem.Key(workItem.Value);
	//}

	public void PumpBackgroundJobs(double milliseconds)
	{
		GPerf.BeginBlock(GPerf.eMajorTraceType.Sync);
		KeyValuePair<SendOrPostCallback, object> workItem;
		var end = GPerf.Now() + GPerf.MsToTicks(milliseconds);
		if (m_queue.Count > 0)
			while (m_queue.TryTake(out workItem, 0))
			{
				GPerf.BeginBlock("Task");
				workItem.Key(workItem.Value);
				GPerf.EndBlock();
				if (GPerf.Now() > end) break;
			}
		GPerf.EndBlock(GPerf.eMajorTraceType.Sync);
	}

	public int TaskCount => m_queue.Count;

	public void Run(Func<Task> asyncMethod)
	{
		var prevCtx = Current;
		try
		{
			SetSynchronizationContext(this);

			Task t0 = asyncMethod();
		}
		catch (Exception e) { GError.RaiseError(e); }
		finally { SetSynchronizationContext(prevCtx); }
	}
	public void Run(Action asyncMethod)
	{
		var prevCtx = Current;
		try
		{
			SetSynchronizationContext(this);

			asyncMethod();
		}
		catch (Exception e) { GError.RaiseError(e); }
		finally { SetSynchronizationContext(prevCtx); }
	}

	public override SynchronizationContext CreateCopy()
	{
		return this;
	}

	// ostensibly I can use this to switch to this context? I'm not convinced this works, and there are more reliable alternatives
	internal SynchronizationContextAwaiter GetSwitchAwaiter() => new SynchronizationContextAwaiter(this);
}
internal struct SynchronizationContextAwaiter : INotifyCompletion
{
	static SendOrPostCallback _postCallback = state => ((Action)state)();
	SynchronizationContext _context;

	public SynchronizationContextAwaiter(SynchronizationContext context)
	{
		_context = context;
	}

	public bool IsCompleted => _context == SynchronizationContext.Current;
	public void OnCompleted(Action continuation) => _context.Post(_postCallback, continuation);
	public void GetResult() { }
}
internal static class SynchronizationContextExtensions
{
	public static SynchronizationContextAwaiter GetAwaiter(this SynchronizationContext context)
	{
		return new SynchronizationContextAwaiter(context);
	}
}
