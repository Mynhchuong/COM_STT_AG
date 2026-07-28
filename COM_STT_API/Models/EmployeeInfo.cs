namespace COM_STT_API.Models;

public class EmployeeInfo
{
    public string EmpCd { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string DeptCd { get; set; } = string.Empty;
    public string? DeptNm { get; set; }
    public string? LineCd { get; set; }
}
