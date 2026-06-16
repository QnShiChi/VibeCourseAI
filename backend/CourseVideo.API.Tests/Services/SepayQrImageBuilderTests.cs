using CourseVideo.API.Models;
using CourseVideo.API.Services;
using FluentAssertions;
using Xunit;

namespace CourseVideo.API.Tests.Services;

public class SepayQrImageBuilderTests
{
    [Fact]
    public void Build_ShouldReturnHostedQrUrl_ForSupportedBank()
    {
        var order = CreateOrder();
        order.BankCode = "Vietcombank";
        order.BankName = "Ngân hàng TMCP Ngoại thương Việt Nam";

        var result = SepayQrImageBuilder.Build(order, "VibeCourseAI", "Live");

        result.Should().StartWith("https://qr.sepay.vn/img?");
        result.Should().Contain("bank=VCB");
        result.Should().Contain("des=VC50845524BA31");
    }

    [Fact]
    public void Build_ShouldReturnFallbackSvg_ForUnsupportedSandboxBank()
    {
        var order = CreateOrder();
        order.BankCode = "ACMEBank";
        order.BankName = "Ngân hàng giả lập";

        var result = SepayQrImageBuilder.Build(order, "VibeCourseAI", "Sandbox");

        result.Should().StartWith("data:image/svg+xml");
        result.Should().Contain(Uri.EscapeDataString("SePay Sandbox"));
        result.Should().Contain(Uri.EscapeDataString("VC50845524BA31"));
    }

    private static PaymentOrder CreateOrder()
    {
        return new PaymentOrder
        {
            Id = Guid.NewGuid(),
            CourseId = Guid.NewGuid(),
            OrderCode = "VC50845524BA31",
            Amount = 599000,
            BankAccountNumber = "7430851296",
            AccountHolderName = "CONG TY CO PHAN FIRETELL",
            TransferContent = "VC50845524BA31"
        };
    }
}
