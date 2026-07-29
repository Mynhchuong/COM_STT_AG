using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using COM_STT_WEB.Models.Account;

namespace COM_STT_WEB.Models.Production;

public class ProductionIndexViewModel
{
    public List<InspectionBatchViewModel> Batches { get; set; } = new();
    public string DateFilter { get; set; } = string.Empty;
    public string StatusFilter { get; set; } = "N";
}

public class InspectionBatchViewModel
{
    [JsonPropertyName("SEQ")]
    public int Seq { get; set; }

    [JsonPropertyName("LINE_CODE")]
    public string? LineCode { get; set; }

    [JsonPropertyName("MACHINE_CODE")]
    public string? MachineCode { get; set; }

    [JsonPropertyName("IP_UPLOAD")]
    public string? IpUpload { get; set; }

    [JsonPropertyName("PROD_TYPE")]
    public int ProdType { get; set; }

    [JsonPropertyName("C_SHIFT")]
    public string? CShift { get; set; }

    [JsonPropertyName("PLANT")]
    public string? Plant { get; set; }

    [JsonPropertyName("D_GATHER")]
    public string? DGather { get; set; }

    [JsonPropertyName("STATUS")]
    public string Status { get; set; } = "N";

    [JsonPropertyName("ORDERS")]
    public string? Orders { get; set; }

    [JsonPropertyName("STYLES")]
    public string? Styles { get; set; }

    [JsonPropertyName("SIZES")]
    public string? Sizes { get; set; }

    [JsonPropertyName("PCARD_NAMES")]
    public string? PcardNames { get; set; }

    [JsonPropertyName("TOTAL_QTY")]
    public int TotalQty { get; set; }

    [JsonPropertyName("ORDER_QTY")]
    public int OrderQty { get; set; }
}

public class BatchRegistrationViewModel
{
    public int Seq { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn Nhà máy (Plant)")]
    public string Plant { get; set; } = "B";

    [Required(ErrorMessage = "Vui lòng chọn Chuyền (Line)")]
    public string LineCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập/quét ít nhất 1 mã máy")]
    public string MachineCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng chọn Ca làm việc")]
    public string CShift { get; set; } = "SHIFT1";

    public bool IsEdit { get; set; }
}

public class PcardScanViewModel
{
    public InspectionBatchViewModel Batch { get; set; } = new();
    public List<PcardScanItemViewModel> Pcards { get; set; } = new();
}

public class PcardScanItemViewModel
{
    [JsonPropertyName("PCARD_NO")]
    public string PcardNo { get; set; } = string.Empty;

    [JsonPropertyName("C_GROUP_SUM")]
    public string? CGroupSum { get; set; }

    [JsonPropertyName("C_STYLE")]
    public string? CStyle { get; set; }

    [JsonPropertyName("C_ORDER")]
    public string? COrder { get; set; }

    [JsonPropertyName("I_PARTS_NO")]
    public string? IPartsNo { get; set; }

    [JsonPropertyName("C_SIZE")]
    public string? CSize { get; set; }

    [JsonPropertyName("SEQ")]
    public int Seq { get; set; }

    [JsonPropertyName("Q_QTY")]
    public int QQty { get; set; }
}
