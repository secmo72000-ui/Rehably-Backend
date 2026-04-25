namespace Rehably.Domain.Enums;

public enum AppointmentStatus
{
    Scheduled  = 0,  // Appointment booked
    Confirmed  = 1,  // Appointment confirmed (patient acknowledged)
    CheckedIn  = 2,  // Reception: patient arrived + payment confirmed
    InProgress = 3,  // Doctor started the session / assessment
    Completed  = 4,  // Assessment submitted / session done
    Cancelled  = 5,
    NoShow     = 6
}
