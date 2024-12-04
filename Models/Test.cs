using System.ComponentModel;

namespace SvgOutputSample.Models
{
#nullable disable
    public class TestViewModel
    {
        [DisplayName("post_code")]
        public string post_code_Text { get; set; }

        [DisplayName("address")]
        public string address_Text { get; set; }

        [DisplayName("name")]
        public string name_Text { get; set; }

        [DisplayName("number")]
        public string number_Text { get; set; }

        [DisplayName("date")]
        public string date_Text { get; set; }

        [DisplayName("municipalities")]
        public string municipalities_Text { get; set; }

        [DisplayName("approval")]
        public string approval_Text { get; set; }

        [DisplayName("c_name")]
        public string c_name_Text { get; set; }

        [DisplayName("c_birth")]
        public string c_birth_Text { get; set; }

        [DisplayName("c_gender")]
        public string c_gender_Text { get; set; }

        [DisplayName("p_post_code")]
        public string p_post_code_Text { get; set; }

        [DisplayName("p_address")]
        public string p_address_TextArea { get; set; }

        [DisplayName("p_name")]
        public string p_name_Text { get; set; }

        [DisplayName("s_approval")]
        public string s_approval_Text { get; set; }

        [DisplayName("e_approval")]
        public string e_approval_Text { get; set; }

        [DisplayName("grant")]
        public string grant_Text { get; set; }

        [DisplayName("c_handicapped")]
        public string c_handicapped_Text { get; set; }

        [DisplayName("c_medical")]
        public string c_medical_Text { get; set; }

        [DisplayName("c_consider")]
        public string c_consider_Text { get; set; }

        [DisplayName("exempt")]
        public string exempt_Text { get; set; }

        [DisplayName("department")]
        public string department_Text { get; set; }


        public TestViewModel()
        {
            post_code_Text = "123-4567";
            address_Text = "東京都渋谷区某通り1-2-3";
            name_Text = "山田 太郎";
            number_Text = "12345";
            date_Text = "2024-11-19";
            municipalities_Text = "渋谷区";
            approval_Text = "承認済み";
            c_name_Text = "山田 花子";
            c_birth_Text = "2010-05-15";
            c_gender_Text = "女性";
            p_post_code_Text = "234-5678";
            p_address_TextArea = "大阪府難波某通り4-5-6";
            p_name_Text = "保護者名";
            s_approval_Text = "保留中";
            e_approval_Text = "却下";
            grant_Text = "支給済み";
            c_handicapped_Text = "いいえ";
            c_medical_Text = "対象";
            c_consider_Text = "特別配慮";
            exempt_Text = "税金免除";
            department_Text = "人事部";
        }


    }
}
