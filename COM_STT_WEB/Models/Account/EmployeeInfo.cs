namespace COM_STT_WEB.Models.Account;

public class EmployeeInfo
{
    public string EmpCd { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string DeptCd { get; set; } = string.Empty;
    public string? DeptNm { get; set; }
    public string? LineCd { get; set; }
}
