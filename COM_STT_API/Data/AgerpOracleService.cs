namespace COM_STT_API.Data;

// DB AGERP (192.168.100.9, SID=agerp, user HRMS) — chỉ dùng để xác thực đăng nhập.
public class AgerpOracleService : OracleServiceBase
{
    public AgerpOracleService(IConfiguration configuration)
        : base(configuration, "OracleDb_AGERP")
    {
    }
}
