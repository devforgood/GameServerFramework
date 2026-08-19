#include "DbMonitorTicker.h"

#include "DbThreadMonitor.h"

void DbMonitorTicker::Fire(Clock::time_point now)
{
	if (now >= nextCheck_)
	{
		DbThreadMonitor::Instance().CheckStuckTasks();
		nextCheck_ = now + kCheckInterval;
	}

	if (now >= nextReport_)
	{
		DbThreadMonitor::Instance().ReportThreadUsage();
		nextReport_ = now + kReportInterval;
	}

	nextDue_ = (nextCheck_ < nextReport_) ? nextCheck_ : nextReport_;
}
