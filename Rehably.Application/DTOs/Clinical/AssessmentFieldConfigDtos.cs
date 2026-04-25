namespace Rehably.Application.DTOs.Clinical;

public record AssessmentFieldConfigDto(
    int StepNumber,
    string FieldKey,
    string FieldLabel,
    bool IsVisible,
    bool IsRequired
);

public record UpdateFieldConfigRequest(
    List<UpdateFieldConfigItem> Fields
);

public record UpdateFieldConfigItem(
    int StepNumber,
    string FieldKey,
    bool IsVisible,
    bool IsRequired
);
