namespace Mentat;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();

		// Strony szczegółów otwierane z parametrami (projectId / meetingId).
		Routing.RegisterRoute("projectDetail", typeof(ProjectDetailPage));
		Routing.RegisterRoute("meetingDetail", typeof(MeetingDetailPage));
	}
}
