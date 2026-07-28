namespace COM_STT_API.Data;

// DB AGMES (192.168.100.10, SID=agmes, user MES) — dùng cho toàn bộ nghiệp vụ sau khi đăng nhập.
public class AgmesOracleService : OracleServiceBase
{
    public AgmesOracleService(IConfiguration configuration)
        : base(configuration, "OracleDb_AGMES")
    {
    }
}
