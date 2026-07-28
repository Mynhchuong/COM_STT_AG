using COM_STT_API.Data;
using COM_STT_API.Models;
using Oracle.ManagedDataAccess.Client;

namespace COM_STT_API.Services;

// Đăng nhập bằng MES.TRTB_M_USER (I_EMP_NO/I_PASSWORD) trên DB AGMES.
// LEFT JOIN sang HRMS.ECM100 bên DB AGERP qua DB link DL_AGERP để lấy họ tên chuẩn (CNAME, font VNI).
// Tài khoản MES không có dòng khớp bên HRMS (VD tài khoản IT/dev nội bộ) vẫn login được,
// chỉ là dùng tạm N_NAME (tên tiếng Anh có sẵn trên TRTB_M_USER) thay cho CNAME,
// và không bị chặn bởi cờ JEAJIKGB (không xác minh được tình trạng đi làm qua HRMS).
public class AuthService
{
    private readonly AgmesOracleService _db;

    public AuthService(AgmesOracleService db)
    {
        _db = db;
    }

    public async Task<LoginResult> ValidateLoginAsync(string empCd, string password)
    {
        empCd = (empCd ?? string.Empty).Trim();
        password = (password ?? string.Empty).Trim();

        if (string.IsNullOrEmpty(empCd) || string.IsNullOrEmpty(password))
            return LoginResult.Fail("Vui lòng nhập mã nhân viên và mật khẩu.");

        const string sql = @"
            SELECT A.I_EMP_NO, A.N_NAME, A.C_DEPT, A.N_DEPT, A.C_LINE,
                   B.CNAME, B.JEAJIKGB
            FROM MES.TRTB_M_USER A
            LEFT JOIN HRMS.ECM100@DL_AGERP B
                   ON A.I_EMP_NO = B.EMPCD
            WHERE A.I_EMP_NO = :empCd
              AND A.I_PASSWORD = :password";

        var rows = await _db.ExecuteQueryAsync(sql, Map,
            new OracleParameter("empCd", empCd),
            new OracleParameter("password", password));
        var row = rows.FirstOrDefault();

        if (row == null)
            return LoginResult.Fail("Mã nhân viên hoặc mật khẩu không đúng.");

        // Chỉ chặn theo JEAJIKGB khi có dòng khớp bên HRMS — không có thì không xác minh được, cho qua.
        if (row.CName != null && !string.Equals(row.JeaJikGb, "Y", StringComparison.OrdinalIgnoreCase))
            return LoginResult.Fail("Nhân viên đã nghỉ việc, không thể đăng nhập.");

        return LoginResult.Ok(row.ToEmployeeInfo());
    }

    public async Task<ChangePasswordResult> ChangePasswordAsync(string empCd, string oldPassword, string newPassword)
    {
        empCd = (empCd ?? string.Empty).Trim();
        oldPassword = (oldPassword ?? string.Empty).Trim();
        newPassword = (newPassword ?? string.Empty).Trim();

        if (string.IsNullOrEmpty(empCd) || string.IsNullOrEmpty(oldPassword) || string.IsNullOrEmpty(newPassword))
            return ChangePasswordResult.Fail("Vui lòng nhập đầy đủ mật khẩu hiện tại và mật khẩu mới.");

        // Cột I_PASSWORD trên MES.TRTB_M_USER là VARCHAR2(10) — mật khẩu mới không được vượt quá độ dài này.
        if (newPassword.Length > 10)
            return ChangePasswordResult.Fail("Mật khẩu mới tối đa 10 ký tự.");

        const string checkSql = @"
            SELECT COUNT(*) AS CNT
            FROM MES.TRTB_M_USER
            WHERE I_EMP_NO = :empCd AND I_PASSWORD = :oldPassword";

        var matches = await _db.ExecuteQueryAsync(checkSql,
            r => Convert.ToInt32(r["CNT"]),
            new OracleParameter("empCd", empCd),
            new OracleParameter("oldPassword", oldPassword));

        if (matches.FirstOrDefault() != 1)
            return ChangePasswordResult.Fail("Mật khẩu hiện tại không đúng.");

        const string updateSql = @"
            UPDATE MES.TRTB_M_USER
               SET I_PASSWORD = :newPassword
             WHERE I_EMP_NO = :empCd";

        await _db.ExecuteNonQueryAsync(updateSql,
            new OracleParameter("newPassword", newPassword),
            new OracleParameter("empCd", empCd));

        return ChangePasswordResult.Ok();
    }

    private sealed record EmployeeRow(
        string EmpCd, string NName, string DeptCd, string? DeptNm, string? LineCd,
        string? CName, string? JeaJikGb)
    {
        public EmployeeInfo ToEmployeeInfo() => new()
        {
            EmpCd = EmpCd,
            FullName = !string.IsNullOrWhiteSpace(CName) ? CName : NName,
            DeptCd = DeptCd,
            DeptNm = DeptNm,
            LineCd = LineCd
        };
    }

    private static EmployeeRow Map(OracleDataReader r) => new(
        EmpCd: r["I_EMP_NO"] as string ?? string.Empty,
        NName: r["N_NAME"] as string ?? string.Empty,
        DeptCd: r["C_DEPT"] as string ?? string.Empty,
        DeptNm: r["N_DEPT"] as string,
        LineCd: r["C_LINE"] as string,
        CName: r["CNAME"] as string,
        JeaJikGb: r["JEAJIKGB"] as string
    );
}
