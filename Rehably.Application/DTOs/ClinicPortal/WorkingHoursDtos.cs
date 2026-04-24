namespace Rehably.Application.DTOs.ClinicPortal;

/// <summary>Single day schedule returned from GET /api/clinic/working-hours</summary>
public record WorkingHoursDayDto(
    int     DayOfWeek,   // 0=Sun … 6=Sat
    string  DayNameAr,
    string  DayNameEn,
    bool    IsOpen,
    string? OpenTime,    // "HH:mm" or null
    string? CloseTime    // "HH:mm" or null
);

/// <summary>PUT /api/clinic/working-hours — update the full weekly schedule at once</summary>
public record UpdateWorkingHoursRequest(List<UpdateWorkingHoursDayDto> Schedule);

public record UpdateWorkingHoursDayDto(
    int     DayOfWeek,
    bool    IsOpen,
    string? OpenTime,
    string? CloseTime
);
