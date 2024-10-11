using System.ComponentModel;

namespace SvgOutputSample.Models
{
    public class Estimate
    {

        [DisplayName("顧客名")]
        public string Name { get; set; } // 顧客名

        [DisplayName("敬称")]
        public string HonorificTitle { get; set; } = "御中"; // 敬称

        [DisplayName("案件名")]
        public string ProjectName { get; set; } // 案件名

        [DisplayName("納品先")]
        public string DeliveryAddress { get; set; } // 納品先

        [DisplayName("予定納期")]
        public string ScheduledDeliveryDate { get; set; } // 予定納期

        [DisplayName("有効期限")]
        public string ExpirationDate { get; set; } // 有効期限

        [DisplayName("税区分")]
        public string TaxCategory { get; set; } = "税込"; // 税区分

        [DisplayName("作成日")]
        public string CreatedDate { get; set; } // 作成日

        [DisplayName("見積コード")]
        public string EstimateCode { get; set; } // 見積コード

        [DisplayName("合計金額")]
        public string TotalMoney { get; set; }

        [DisplayName("支店住所")]
        public string Addres { get; set; }

        [SvgTextAnchor(SvgTextAnchorAttribute.End)]
        [DisplayName("小計")]
        public decimal Subtotal { get; set; } = 0; // 小計

        [SvgTextAnchor(SvgTextAnchorAttribute.End)]
        [DisplayName("値引き")]
        public string Discount { get; set; } // 値引き

        [SvgTextAnchor(SvgTextAnchorAttribute.End)]
        [DisplayName("合計")]
        public string Total { get; set; } // 値引き

        [SvgTextAnchor(SvgTextAnchorAttribute.End)]
        [DisplayName("消費税")]
        public string ConsumptionTax { get; set; }

        [SvgTextAnchor(SvgTextAnchorAttribute.End)]
        [DisplayName("税込合計")]
        public string TotalIncludingTax { get; set; }

        [DisplayName("備考")]
        public string Remarks_TextArea { get; set; }

        [DisplayName("コメント")]
        public string Comment_TextArea { get; set; }

        public List<EstimateItem> Items { get; set; }

        public Estimate()
        {
            Random random = new Random();
            int randomRepeats = random.Next(1, 11);

            Name = "株式会社" + new string('ほ', randomRepeats) + new string('げ', randomRepeats);
            ProjectName = "案件" + new string('ふ', randomRepeats) + new string('が', randomRepeats);
            DeliveryAddress = new string('ぴ', randomRepeats) + "支店オフィス";
            ScheduledDeliveryDate = DateTime.Today.AddMonths(1).ToString("yyyy/MM/dd");
            ExpirationDate = DateTime.Today.ToString("yyyy/MM/dd");
            CreatedDate = DateTime.Now.ToString("yyyy/MM/dd");
            EstimateCode = $"EST-{DateTime.Now:yyyyMMdd}-001";
            Discount = "10,000";
            Total = "234,000";
            Addres = "北海道札幌市";
            TotalMoney = "320,000,000";
            ConsumptionTax = "10,000";
            TotalIncludingTax = "100,000,000,000,000";
            Remarks_TextArea = "テステストテストテストテストテストテストテストテストテストテストテストテストテストテテストテストテストテストテテストテストテストテストテテストテストテストテストテテストテストテストテストテテストテストテストテストテテストテストテストテストテテストテストテストテストテテストテストテストテストテテストテストテストテストテストテストテストテストテストテストテストテストテストテストテストテストテストテストテストテストテストテストテストテストテストテストテストテストテストテストテストテストテストテストテストテストテストテストテストテストテストテストテストテストテストテスト" +
                "テストテストテストテストテストテストテストテストテストテストテストテストテストテストテストテストテストテストテストテストテストテストテストテストテストテストテストテストテストテストテストテストテストテストテストテストテスト";
            Comment_TextArea = "テステストテストテストテストテストテストテストテストテストテストテストテストテストテテストテストテストテストテテストテストテストテストテテストテストテストテストテテストテストテストテストテテストテストテストテストテテストテストテストテストテテストテストテストテストテテストテストテストテストテテストテストテストテストテストテストテストテストテストテストテストテストテストテストテストテストテストテストテストテストテストテストテストテストテストテストテストテストテストテストテストテストテストテストテストテストテストテストテストテストテストテストテストテストテストテスト\" +\r\n                \"テストテストテストテストテストテストテストテストテストテストテストテストテストテストテストテストテストテストテストテストテストテストテストテストテストテストテストテストテストテストテストテストテストテストテストテストテスト";
            Items = new List<EstimateItem>();
            decimal subtotal = 0;

            for (int i = 0; i < 26; i++)
            {
                var item = new EstimateItem(random);
                item.Index = i;
                Items.Add(item);
                subtotal += Convert.ToDecimal(item.Amount.Replace(",", ""));
            }
        }
    }

    public class EstimateItem
    {
        public int Index { get; set; }
        [DisplayName("商品名")]
        public string ProductName { get; set; }

        [DisplayName("メーカー名")]
        public string ManufacturerName { get; set; }

        [DisplayName("型番")]
        public string ModelNumber { get; set; }

        [DisplayName("数量")]
        public int Quantity { get; set; }

        [DisplayName("単位")]
        public string Unit { get; set; }

        [SvgTextAnchor(SvgTextAnchorAttribute.End)]
        [DisplayName("単価")]
        public string UnitPrice { get; set; }

        [SvgTextAnchor(SvgTextAnchorAttribute.End)]
        [DisplayName("金額")]
        public string Amount { get; set; }

        public EstimateItem(Random random)
        {
            ProductName = new string('商', random.Next(1, 11)) + new string('品', random.Next(1, 11)) + "名";
            ManufacturerName = new string('メ', random.Next(1, 3)) + "ーカー名";
            ModelNumber = new string('型', random.Next(1, 6)) + "番";
            Quantity = random.Next(1, 11);
            Unit = new List<string> { "個", "台", "セット" }[random.Next(0, 3)];

            decimal unitPrice = random.Next(1, 101) * 1000;
            decimal amount = unitPrice * Quantity;

            UnitPrice = string.Format("{0:N0}", unitPrice);
            Amount = string.Format("{0:N0}", amount);
        }
    }
}
