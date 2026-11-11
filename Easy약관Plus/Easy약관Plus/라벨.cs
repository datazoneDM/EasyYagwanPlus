using iText.IO.Font;
using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.Kernel.Pdf;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.Layout;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using iText.Forms;
using System.Windows.Forms;

namespace Easy약관Plus
{
    
    class 라벨
    {
        public 라벨()
        {

        }

        // ==================
        //        라벨
        // ==================

        /// <summary>
        /// 라벨출력
        /// </summary>
        public void 라벨출력(string[] 리스트항목, PdfDocument 묶음증권doc, Document docW, string 고객사)
        {
            string 라벨경로 = string.Format("{0}\\라벨.pdf", Application.StartupPath); 
            FileStream fsLabel = new FileStream(라벨경로, FileMode.Open, FileAccess.Read, FileShare.Read);
            PdfDocument tmp라벨 = new PdfDocument(new PdfReader(fsLabel));

            // 개별증권을 먼저 묶고 라벨을 앞에 추가
            tmp라벨.CopyPagesTo(1, 2, 묶음증권doc,1, new PdfPageFormCopier());

            tmp라벨.Close();
            fsLabel.Close();

            int 라벨인수 = 1;

            string 품명 = string.Format("({0:MM/dd}) 보험증권", DateTime.Now); 
            string 파일명 = 리스트항목[5] ?? "";

            System.Globalization.CultureInfo 한글 = System.Globalization.CultureInfo.CreateSpecificCulture("ko-KR");

            DateTime 작업일 = DateTime.Now;
            DateTime 발송일;
            string 작업요일 = 작업일.ToString(string.Format("ddd", 한글));
            switch (작업요일) {
                case "목":
                case "금": 
                    발송일 = 작업일.AddDays(4);
                    break;
                case "토":
                    발송일 = 작업일.AddDays(3);
                    break;
                default:
                    발송일 = 작업일.AddDays(2);
                    break;
            }
            string 한글발송일 = Convert.ToDateTime(발송일).ToString(string.Format("MM월 dd일(ddd)", 한글)); // 3월 5일(금)
            string 발송형태 = "일반";
            string 구성P = 리스트항목[6] ?? "";  // 값이 null 이면 공백 ( ??을 기준으로 왼쪽의 값이 Null 이면 오른쪽값 대입)
            string 폼지 = 리스트항목[81] ?? "";
            string 약관 = 리스트항목[111] ?? "";
            string 봉투 = 리스트항목[131] ?? "";
            string 전달사항 = 리스트항목[13] ?? "";
            string OP = "";
            string IMS = "";
            string 수작업 = "";
            string 고객명 = "";
            string 우편번호 = "";
            string 일련번호 = string.Format("{0} ~ {1}", 리스트항목[7], 리스트항목[8]);


            PdfFont 나눔바른고딕 = PdfFontFactory.CreateFont(@"c:\Easy약관Plus\program\Font\NanumBarunGothic.ttf", PdfEncodings.IDENTITY_H);
            docW.SetFontSize(18);
            docW.SetFont(나눔바른고딕);
            docW.SetFontColor(ColorConstants.BLACK);

            string tmp고객사 = "";
            switch (고객사.ToUpper())
            {
                case "MG":
                    tmp고객사 = "MG손해보험";
                    break;
                case "KB":
                    tmp고객사 = "KB손해보험";
                    break;
                case "AI":
                    tmp고객사 = "AIG손해보험";
                    break;
                case "MZ":
                    tmp고객사 = "메리츠화재";
                    break;
                case "CH":
                    tmp고객사 = "처브화재";
                    break;
                case "LT":
                    tmp고객사 = "롯데손해보험";
                    break;
                case "DB":
                    tmp고객사 = "DB손해보험";
                    break;
                case "DZ":
                    tmp고객사 = "데이타존";
                    break;
            }




            double X위치 = 23.0;
            docW.ShowTextAligned(new Paragraph(tmp고객사), MMtoPoint(X위치), MMtoPoint(297 - 180), 라벨인수, TextAlignment.CENTER, VerticalAlignment.BOTTOM, (float)(Math.PI / 2)); // 고객사
            docW.ShowTextAligned(new Paragraph(품명), MMtoPoint(X위치), MMtoPoint(297 - 56), 라벨인수, TextAlignment.CENTER, VerticalAlignment.BOTTOM, (float)(Math.PI / 2)); // 회전 2: 90도, 회전 6 : 45도
            X위치 += 12.4;
            docW.ShowTextAligned(new Paragraph(파일명), MMtoPoint(X위치), MMtoPoint(297 - 56), 라벨인수, TextAlignment.CENTER, VerticalAlignment.BOTTOM, (float)(Math.PI / 2));
            X위치 += 12.4;
            docW.ShowTextAligned(new Paragraph(한글발송일), MMtoPoint(X위치), MMtoPoint(297 - 202), 라벨인수, TextAlignment.CENTER, VerticalAlignment.BOTTOM, (float)(Math.PI / 2));
            docW.ShowTextAligned(new Paragraph(발송형태), MMtoPoint(X위치), MMtoPoint(297 - 101.2), 라벨인수, TextAlignment.CENTER, VerticalAlignment.BOTTOM, (float)(Math.PI / 2));
            docW.ShowTextAligned(new Paragraph(구성P), MMtoPoint(X위치), MMtoPoint(297 - 28), 라벨인수, TextAlignment.CENTER, VerticalAlignment.BOTTOM, (float)(Math.PI / 2));

            X위치 += 12.4;
            docW.ShowTextAligned(new Paragraph(폼지), MMtoPoint(X위치), MMtoPoint(297 - 128), 라벨인수, TextAlignment.CENTER, VerticalAlignment.BOTTOM, (float)(Math.PI / 2));
            X위치 += 12.4;
            X위치 += 12.4;
            X위치 += 12.4;
            docW.ShowTextAligned(new Paragraph(약관), MMtoPoint(X위치), MMtoPoint(297 - 128), 라벨인수, TextAlignment.CENTER, VerticalAlignment.BOTTOM, (float)(Math.PI / 2));
            X위치 += 12.4;
            docW.ShowTextAligned(new Paragraph(봉투), MMtoPoint(X위치), MMtoPoint(297 - 128), 라벨인수, TextAlignment.CENTER, VerticalAlignment.BOTTOM, (float)(Math.PI / 2));

            X위치 += 23.4;
            docW.ShowTextAligned(new Paragraph(전달사항), MMtoPoint(X위치), MMtoPoint(297 - 128), 라벨인수, TextAlignment.CENTER, VerticalAlignment.BOTTOM, (float)(Math.PI / 2));

            X위치 += 16.4;
            docW.ShowTextAligned(new Paragraph(OP), MMtoPoint(X위치), MMtoPoint(297 - 128), 라벨인수, TextAlignment.CENTER, VerticalAlignment.BOTTOM, (float)(Math.PI / 2));
            X위치 += 14.1;
            docW.ShowTextAligned(new Paragraph(IMS), MMtoPoint(X위치), MMtoPoint(297 - 128), 라벨인수, TextAlignment.CENTER, VerticalAlignment.BOTTOM, (float)(Math.PI / 2));
            X위치 += 14.1;
            docW.ShowTextAligned(new Paragraph(수작업), MMtoPoint(X위치), MMtoPoint(297 - 128), 라벨인수, TextAlignment.CENTER, VerticalAlignment.BOTTOM, (float)(Math.PI / 2));
            X위치 += 12.0;
            docW.ShowTextAligned(new Paragraph(고객명), MMtoPoint(X위치), MMtoPoint(297 - 217), 라벨인수, TextAlignment.CENTER, VerticalAlignment.BOTTOM, (float)(Math.PI / 2));
            docW.ShowTextAligned(new Paragraph(우편번호), MMtoPoint(X위치), MMtoPoint(297 - 131.1), 라벨인수, TextAlignment.CENTER, VerticalAlignment.BOTTOM, (float)(Math.PI / 2));
            docW.ShowTextAligned(new Paragraph(일련번호), MMtoPoint(X위치), MMtoPoint(297 - 41.2), 라벨인수, TextAlignment.CENTER, VerticalAlignment.BOTTOM, (float)(Math.PI / 2));


            // 롤페이지cnt;
            int 롤페이지cnt = 1;
            docW.ShowTextAligned(new Paragraph(string.Format("{0:000000}", 롤페이지cnt)), MMtoPoint(100), MMtoPoint(1.5), 라벨인수, TextAlignment.LEFT, VerticalAlignment.BOTTOM, 0);
            롤페이지cnt += 1;
            docW.ShowTextAligned(new Paragraph(string.Format("{0:000000}", 롤페이지cnt)), MMtoPoint(100), MMtoPoint(1.5), 라벨인수 + 1, TextAlignment.LEFT, VerticalAlignment.BOTTOM, 0);

            docW.Close();
            묶음증권doc.Close();

        }
        private float MMtoPoint(double x)
        {
            float tmpx;
            tmpx = (float)(x * 2.83465);
            return tmpx;
        }
    }
}
