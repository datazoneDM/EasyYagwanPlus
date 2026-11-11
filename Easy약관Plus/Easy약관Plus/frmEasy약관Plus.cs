using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Net.NetworkInformation;
using Microsoft.Win32;
using System.Net;
using System.Globalization;
using System.Security.Cryptography;
using DevExpress.XtraEditors.Repository;
using System.Data.SqlClient;
using System.Configuration;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Easy약관Plus
{
    public partial class frmEasy약관 : DevExpress.XtraBars.Ribbon.RibbonForm
    {
        private string 파라메타 = "";
        private string 기본타이틀 = "Easy약관Plus+ ver2.4";

        public frmEasy약관(string[] args)
        {
            InitializeComponent();
            if (args.Length >= 1)
                파라메타 = args[0];

            
        }

        private string 약관DB = ConfigurationManager.ConnectionStrings["약관DB"].ConnectionString;


        //frm담보관리 Event ( frmEasy약관 ==> frm담보관리 )
        public string[] 담보관리event = new string[10];
        public static event 담보관리_Handler 담보관리_SetMessage;
        public delegate void 담보관리_Handler(string[] 담보관리event);

        //frm담보Merge Event ( frmEasy약관 ==> frm담보Merge )
        public string[] 담보MergeEvent = new string[10];
        public static event 담보Merge_Handler 담보Merge_SetMessage;
        public delegate void 담보Merge_Handler(string[] 담보MergeEvent);

        //frm담보분리 Event ( frmEasy약관 ==> frm담보분리 )
        public string[] 담보분리Event = new string[10];
        public static event 담보분리_Handler 담보분리_SetMessage;
        public delegate void 담보분리_Handler(string[] 담보분리Event);

        //frme한글수정 Event ( frmEasy약관 ==> frm한글수정 )
        public string[] 한글수정Event = new string[10];
        public static event 한글수정_Handler 한글수정_SetMessage;
        public delegate void 한글수정_Handler(string[] 한글수정Event);


        private string 시간표시메뉴 = "";
        private DateTime 시작시간;
        private Timer 소요시간Timer = new Timer();
        private Timer 반복Timer = new Timer();
        private string 메세지 = "";

        private string Sha512Key = "kwakilho!!@@##$$%%^^&&**datazone";//16,24,32 중 택1
        private string Sha512IV = "datazonekwakilho";
        private string 약관형태 = "";

        private void frmEasy약관_Load(object sender, EventArgs e)
        {
            string 사용컴퓨터 = SystemInformation.ComputerName.ToLower();

            if (사용컴퓨터 == "kbmain") // || 사용컴퓨터 == "dzmain"
            {
                폼초기위치사이즈적용();
            }
            else
            {
                this.WindowState = FormWindowState.Maximized;
            }
            기본타이틀 = this.Text;
            // 프로그램이 켜져 있는 동안 절대 잠들지 않게 설정
            PreventSleep.DonSleep();

            타이틀업데이트();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // Panel_Fill 안에 들어있는 컨트롤들을 뒤져서 폼(Form)이 있으면 수동으로 닫아줍니다.
            foreach (Control cont in Panel_Fill.Controls)
            {
                if (cont is Form childForm)
                {
                    // 이 Close() 명령이 호출되어야만 비로소 frm담보Merge의 OnFormClosing이 실행됩니다!
                    childForm.Close();
                }
            }

            base.OnFormClosing(e);
        }

        private void 폼초기위치사이즈적용()
        {
            string dbFilePath = @"c:\Easy약관Plus\program\localDB.txt";

            // 파일이 존재하지 않으면 기본 위치/크기 유지
            if (!File.Exists(dbFilePath)) return;

            try
            {
                // 파일을 한글 인코딩(euc-kr)으로 모두 읽어옵니다.
                string[] lines = File.ReadAllLines(dbFilePath, Encoding.GetEncoding(949));

                foreach (string line in lines)
                {
                    // 탭('\t') 기준으로 문자열을 분리합니다.
                    string[] parts = line.Split('\t');

                    // "위치크기정보" 항목이고, 데이터가 3개(항목명, 키값, 데이터) 이상인지 확인
                    // 키값이 "1"인지 추가로 확인합니다.
                    if (parts.Length >= 3 && parts[0].Trim() == "위치크기정보" && parts[1].Trim() == "1")
                    {
                        // 실제 위치/크기 데이터(X|Y|W|H)는 parts[2]에 있습니다.
                        string[] 위치크기정보 = parts[2].Split('|');

                        // Split된 데이터가 4개 이상인지 확인 후 변환
                        if (위치크기정보.Length >= 4 &&
                            int.TryParse(위치크기정보[0].Trim(), out int x) &&
                            int.TryParse(위치크기정보[1].Trim(), out int y) &&
                            int.TryParse(위치크기정보[2].Trim(), out int width) &&
                            int.TryParse(위치크기정보[3].Trim(), out int height))
                        {
                            // [중요] 코드로 위치를 변경하려면 StartPosition을 Manual로 설정해야 합니다.
                            this.StartPosition = FormStartPosition.Manual;
                            this.WindowState = FormWindowState.Normal;

                            // 읽어온 값으로 폼의 위치와 사이즈 설정
                            this.Location = new Point(x, y);
                            this.Size = new Size(width, height);

                            break; // 원하는 값을 찾고 적용했으므로 루프 종료
                        }
                    }
                }
            }
            catch (Exception)
            {
                // 파일 읽기 권한 오류나 형식 오류 발생 시 
                // 폼이 켜지지 않는 치명적인 에러를 막기 위해 조용히 넘어갑니다.
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            this.Visible = false;  // 처음엔 보이지 않게

            이미지1.EditValue = Image.FromFile(string.Format("{0}\\img\\특허증.PNG", Application.StartupPath));
            이미지2.EditValue = Image.FromFile(string.Format("{0}\\img\\좀비2.PNG", Application.StartupPath));
            이미지3.EditValue = Image.FromFile(string.Format("{0}\\img\\좀비3.PNG", Application.StartupPath));
            이미지4.EditValue = Image.FromFile(string.Format("{0}\\img\\좀비5.PNG", Application.StartupPath));

            frm담보관리.담보관리메인화면_SetMessage += new frm담보관리.담보관리메인화면_Handler(event_메인화면_setMessage);           //<== 담보관리폼에서   메인화면으로 메세지 받음
            frm담보Merge.담보Merge메인화면_SetMessage += new frm담보Merge.담보Merge메인화면_Handler(event_메인화면_setMessage);       //<== 담보Merge폼에서  메인화면으로 메세지 받음
            frm담보분리.담보분리메인화면_SetMessage += new frm담보분리.담보분리메인화면_Handler(event_메인화면_setMessage);           //<== 담보분리폼에서   메인화면으로 메세지 받음
            frm한글수정.한글수정메인화면_SetMessage += new frm한글수정.한글수정메인화면_Handler(event_메인화면_setMessage);           //<== 한글수정폼에서   메인화면으로 메세지 받음



            Form 현재폼 = new Form();
            현재폼 = new frm담보Merge(파라메타);
            작업폼제어(현재폼);


            소요시간Timer.Interval = 1000;
            소요시간Timer.Tick += new EventHandler(소요시간Timer_Tick);

            반복Timer.Interval = 1000;
            반복Timer.Tick += new EventHandler(반복Timer_Tick);


            chk옵저버.EditValue = true;

            //string 레지스트리경로 = @"Software\HNC\HwpCtrl\Modules";
            //string 한글DLL경로 = @"c:\Windows\FilePathCheckerModuleExample.dll";

            //RegistryKey rkey = Registry.CurrentUser.OpenSubKey(레지스트리경로);
            //string get한글DLL경로 = rkey.GetValue("FilePathCheckerModuleExample").ToString();
            //if (rkey == null || get한글DLL경로 != 한글DLL경로) {
            //    HNC레지스트리등록(레지스트리경로, 한글DLL경로);
            //}

            진행바초기화(bar진행상황);
            진행바초기화(bar분석중1);
            진행바초기화(bar분석중2);
            진행바초기화(bar한글비교중);

            진행바초기화(barFTP현재진행);
            진행바초기화(barFTP전체진행);
            bar상품삭제.Enabled = false;

            개정년초기화();


            //string 정상완료 = 라이센스유효성();
            //if (정상완료 == "오류")
            //{
            //    Application.Exit();
            //}

            //this.Text = 정상완료;

            //기본조작변경();

            bar약관형태.EditValue = "분리담보";
            bar담보비교.Caption = "담보비교";
            bar담보upload.Caption = "담보upload";
            약관형태 = "분리담보";
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            타이틀업데이트();
        }

        protected override void OnLocationChanged(EventArgs e)
        {
            base.OnLocationChanged(e);
            타이틀업데이트();
        }

        protected override  void OnShown(EventArgs e)
        {
            base.OnShown(e);
            this.Visible = true;   // 모든 준비가 끝난 후 폼을 한 번에 보여줌
        }

        private void 맞춤약관시작()
        {
            if (barinputpath.EditValue != null && !string.IsNullOrEmpty(barinputpath.EditValue.ToString()) &&
                baroutputpath.EditValue != null && !string.IsNullOrEmpty(baroutputpath.EditValue.ToString()))
            {
                this.BeginInvoke(new MethodInvoker(delegate
                {
                    try
                    {
                        // 토글 버튼 ON
                        bar작업시작.Down = true;

                        // 작업 시작 (이때부터 실제 작업이 돌면서 UI가 뻑뻑해질 수 있습니다)
                        bar작업시작_ItemClick(null, null);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"자동 시작 오류: {ex.Message}");
                    }
                }));
            }
        }
        private void 타이틀업데이트()
        {
            // 창이 최소화된 상태면 무시
            if (this.WindowState == FormWindowState.Minimized)
                return;

            string 현재타이틀 = this.Text;

            // 이전에 붙였던 " - (" 가 존재하는지 확인
            int 구분자위치 = 현재타이틀.IndexOf(" - (");

            // 이미 좌표가 붙어 있다면 그 앞까지만 잘라내고, 없으면 그대로 사용 (순수 타이틀 추출)
            string 순수타이틀 = 구분자위치 > 0 ? 현재타이틀.Substring(0, 구분자위치) : 현재타이틀;

            // 순수 타이틀 뒤에 새 좌표와 사이즈 부착
            this.Text = $"{순수타이틀} - ({this.Location.X}, {this.Location.Y}), ({this.Size.Width}, {this.Size.Height})";
        }


        private void 기본조작변경()
        {
            mnu기본메뉴.SelectedPage = mnu담보분리;
        }

        private void HNC레지스트리등록(string 레지스트리경로, string 한글DLL경로)
        {
            RegistryKey rkey = Registry.CurrentUser.CreateSubKey(레지스트리경로);  // 상위 폴더가 없으면 생성 후 하위폴더가 자동 생성 된다.
            rkey.SetValue("FilePathCheckerModuleExample", 한글DLL경로);
        }

        private string 라이센스유효성()
        {
            string 표시사용기간 = "";
            string 보조라이센스경로 = @"c:\Windows\EasyLicense.lic";

            /* ==== [ 구  조 ] =====
             * 맥주소:00FFCF5BB5BA
             * 시작:20201224181346
             * 기간:2
             * 현재:20201224181346
             * 
             */

            //*.datazone파일 없으면 오류(최신것으로 가져온다.)
            string 대상폴더 = string.Format("{0}\\", Application.StartupPath);
            DirectoryInfo 폴더 = new DirectoryInfo(대상폴더);
            string 입력파일 = "";
            try {
                입력파일 = 폴더.GetFiles("*.datazone").OrderByDescending(f => f.CreationTime).First().FullName;
            } catch (Exception) {
                MessageBox.Show("License가 존재 하지 않습니다.\nLicense을 등록 후 사용 하세요.", "라이센스문의(datazone천재소년)", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return "오류";
            }



            string 메세지 = "";
            string tmp라이센스 = "";
            string[] 라이센스;

            라이센스AESDecrypt(입력파일, CryptoAction.복호화, out 메세지, out 라이센스);

            if (메세지 != "완료") {
                MessageBox.Show(메세지, "라이센스오류(datazone천재소년)", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return "오류";
            }

            List<string> User맥주소 = new List<string>();
            macAddr가져오기(out User맥주소);
            if (User맥주소.Count == 0) {
                MessageBox.Show("User MacAddress Exception", "MacAddress오류(datazone천재소년)", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return "오류";
            }

            string 출력파일 = 입력파일;

            //최초등록 또는 초기화
            //설치 후 처음 라이센스 등록 또는 초기화 - 처음 설치 후 바로 사용하기 위해 맥주소 등록 하고 라이센스를 만들면 시간이 걸려서
            //Open License (보안주의)
            if (매개변수가져오기("맥주소:", 라이센스) == "천재소년") {

                라이센스 = new string[5];
                라이센스[0] = string.Format("맥주소:{0}", User맥주소[0]);              // 해당PC 맥주소
                라이센스[1] = string.Format("시작:{0:yyyyMMddHHmmss}", DateTime.Now);  // 생성시간
                라이센스[2] = string.Format("기간:{0}", 2);                            // 사용기간(개월)
                라이센스[3] = string.Format("현재:{0:yyyyMMddHHmmss}", DateTime.Now);  // 생성시간
                라이센스[4] = "";

                tmp라이센스 = string.Join(Environment.NewLine, 라이센스);

                라이센스AESEncrypt(출력파일, CryptoAction.암호화, tmp라이센스, out 메세지);

                if (메세지 != "완료") {
                    MessageBox.Show(메세지, "라이센스문의(datazone천재소년)", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return "오류";
                }

                //EasyLicense.lic 생성
                출력파일 = 보조라이센스경로;
                tmp라이센스 = 매개변수가져오기("시작:", 라이센스);

                라이센스AESEncrypt(출력파일, CryptoAction.암호화, tmp라이센스, out 메세지);

                if (메세지 != "완료") {
                    MessageBox.Show("Sub License01 - " + 메세지, "라이센스문의(datazone천재소년)", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return "오류";
                }

                //datazone Easy약관Plus+
                표시사용기간 = string.Format("datazone Easy약관Plus+  사용기간: {0}일", Convert.ToInt32(라이센스[2]) * 30);
            } else {
                //라이센스 처음등록하거나 라이센스 기간연장 한는 경우 - '현재'가 존재하지 않는다.
                //라이센스보조 File을 읽어 공백 이거나 비교해서 라이센스보조File이 작으면 등록한다. 만약 같거나 크면 오류처리(같으면 다시 사용을 의미한다.)
                //c:\Windows\EasyLicense.lic - 처음 설치시(맥주소:천재소년), 라이센스 처음등록, 라이센스 기간연장을 할 때는 반드시 EasyLicense.lic 생성해야 한다.

                string tmp이전일 = 매개변수가져오기("현재:", 라이센스);
                Int64 현재일 = Convert.ToInt64(string.Format("{0:yyyyMMddHHmmss}", DateTime.Now));

                if (string.IsNullOrWhiteSpace(tmp이전일)) {

                    입력파일 = 보조라이센스경로;  //@"c:\Windows\EasyLicense.lic"
                    string[] 라이센스보조;

                    if (new FileInfo(입력파일).Length != 0) {
                        라이센스AESDecrypt(입력파일, CryptoAction.복호화, out 메세지, out 라이센스보조);
                        if (메세지 != "완료" || (라이센스보조[0] != "" && 라이센스보조[0] == 매개변수가져오기("시작:", 라이센스))) {
                            MessageBox.Show("기간이 만료된 License 입니다.", "라이센스문의(datazone천재소년)", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return "오류";
                        }
                    }

                    //EasyLicense.lic 생성
                    출력파일 = 입력파일;
                    tmp라이센스 = 매개변수가져오기("시작:", 라이센스);

                    라이센스AESEncrypt(출력파일, CryptoAction.암호화, tmp라이센스, out 메세지);

                    if (메세지 != "완료") {
                        MessageBox.Show("Sub License02 - " + 메세지, "라이센스문의(datazone천재소년)", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return "오류";
                    }
                } else {
                    //현재 라이센스파일 유효성 검사
                    Int64 이전일 = Convert.ToInt64(매개변수가져오기("현재:", 라이센스)); // 프로그램을 마지막 사용일

                    // 시스템 날짜가 과거로 되어 있을때
                    if (이전일 > 현재일) {
                        // 현재System날짜가 이전종료 한 날짜보다 작으면 날짜를 임의로 과거로 바꾼것으로 인식해 인터넷 표준 시간을 가져오거나 아님 오류로 처리
                        DateTime dateTime;
                        using (var response = WebRequest.Create("http://www.google.com").GetResponse())
                        {
                            dateTime = DateTime.ParseExact(response.Headers["date"], "ddd, dd MMM yyyy HH:mm:ss 'GMT'", CultureInfo.InvariantCulture.DateTimeFormat, DateTimeStyles.AssumeUniversal);
                            현재일 = Convert.ToInt64(string.Format("{0:yyyyMMddHHmmss}", dateTime));
                        }
                        if (이전일 > 현재일) {
                            MessageBox.Show("License Exception 날짜오류", "라이센스문의(datazone천재소년)", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return "오류";
                        }
                    }
                }

                //라이센스가 종료 되었을때
                Int64 기간 = Convert.ToInt64(매개변수가져오기("기간:", 라이센스));
                string tmp시간일 = 매개변수가져오기("시작:", 라이센스).Substring(0, 8).Insert(4, "-").Insert(7, "-"); // 프로그램을 마지막 사용일
                DateTime dt시간일 = Convert.ToDateTime(tmp시간일);
                TimeSpan 사용기간 = DateTime.Now - dt시간일;
                Int64 남은기간 = (기간 * 30) - 사용기간.Days;
                if (남은기간 > 기간 * 30 || 남은기간 < 0) {
                    MessageBox.Show("License 사용기간이 만료 되었습니다.", "라이센스문의(datazone천재소년)", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return "오류";
                }

                표시사용기간 = string.Format("datazone Easy약관Plus+  사용기간: {0}일", 남은기간);

                //맥어드레스가 상이한 경우
                string 등록된맥주소 = 매개변수가져오기("맥주소:", 라이센스);
                int 존재여부 = User맥주소.IndexOf(등록된맥주소);
                if (존재여부 == -1) {
                    MessageBox.Show("등록되어 있는 사용기기가 아닙니다.", "라이센스문의(datazone천재소년)", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return "오류";
                }

                int 현재위치 = Array.FindIndex(라이센스, i => i.Contains("현재:"));
                if (현재위치 == -1) {
                    라이센스[라이센스.GetUpperBound(0)] = "현재:" + 현재일.ToString();
                } else {
                    라이센스[현재위치] = "현재:" + 현재일.ToString();
                }

                tmp라이센스 = string.Join(Environment.NewLine, 라이센스);

                라이센스AESEncrypt(출력파일, CryptoAction.암호화, tmp라이센스, out 메세지);

                if (메세지 != "완료") {
                    MessageBox.Show(메세지, "라이센스문의(datazone천재소년)", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return "오류";
                }
            }
            return 표시사용기간;
        }
        private string 매개변수가져오기(string 매개변수, string[] 라이센스)
        {
            매개변수 = Array.Find(라이센스, result => result.StartsWith(매개변수, StringComparison.OrdinalIgnoreCase)); // 대소문자 구분하지 않음
            매개변수 = (매개변수 == null) ? "" : 매개변수.Substring(매개변수.IndexOf(':') + 1);
            return 매개변수;
        }

        private void macAddr가져오기(out List<string> User맥주소)
        {
            User맥주소 = new List<string>();

            /*
             string 맥주소 = "";
             var macaddr = (
                 from nic in NetworkInterface.GetAllNetworkInterfaces()
                 where nic.OperationalStatus == OperationalStatus.Up
                 select nic.GetPhysicalAddress().ToString()
                 ).FirstOrDefault();

             맥주소 = macaddr; */

            NetworkInterface[] nics = NetworkInterface.GetAllNetworkInterfaces();
            foreach (NetworkInterface nic in nics) {
                string tmp맥주소 = nic.GetPhysicalAddress().ToString();
                if (!string.IsNullOrEmpty(tmp맥주소)) {
                    if (!User맥주소.Contains(tmp맥주소)) {
                        User맥주소.Add(tmp맥주소);
                    }
                }
            }
        }

        //복호화
        private void 라이센스AESDecrypt(string strInputFile, CryptoAction 구분, out string 메세지, out string[] 라이센스)
        {
            메세지 = "완료";
            라이센스 = new string[5];
            try
            {
                RijndaelManaged aes = new RijndaelManaged();
                aes.KeySize = 256;
                aes.BlockSize = 128;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                aes.Key = Encoding.UTF8.GetBytes(Sha512Key);
                aes.IV = Encoding.UTF8.GetBytes(Sha512IV);
                var decrypt = aes.CreateDecryptor();
                byte[] xBuff = null;

                FileStream fsInput = new FileStream(strInputFile, FileMode.Open, FileAccess.Read);
                StreamReader fsRead = new StreamReader(fsInput, Encoding.GetEncoding(949));

                using (var ms = new MemoryStream()) {
                    using (var cs = new CryptoStream(ms, decrypt, CryptoStreamMode.Write)) {
                        byte[] xXml = Convert.FromBase64String(fsRead.ReadToEnd());
                        cs.Write(xXml, 0, xXml.Length);
                    }
                    xBuff = ms.ToArray();
                }

                String Output = Encoding.GetEncoding(949).GetString(xBuff);
                라이센스 = Output.Replace("\r", "").Split('\n');

                fsRead.Close(); fsInput.Close();
            }
            catch (IOException) {
                메세지 = string.Format("해당 라이센스파일을 생성 하는데 실패 했습니다. {0}", 구분);
                return;
            } catch (Exception e) {
                메세지 = string.Format("해당 라이센스파일을 생성 하는데 실패 했습니다. {0}\n{1}", 구분, e.Message);
                return;
            }


        }

        //암호화
        private void 라이센스AESEncrypt(string strOutputFile, CryptoAction 구분, string tmp라이센스, out string 메세지)
        {
            메세지 = "완료";
            try
            {
                RijndaelManaged aes = new RijndaelManaged();
                aes.KeySize = 256;
                aes.BlockSize = 128;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                aes.Key = Encoding.UTF8.GetBytes(Sha512Key);
                aes.IV = Encoding.UTF8.GetBytes(Sha512IV);
                var encrypt = aes.CreateEncryptor(aes.Key, aes.IV);
                byte[] xBuff = null;

                FileStream fsOutput = new FileStream(strOutputFile, FileMode.OpenOrCreate, FileAccess.Write);
                StreamWriter fsWrite = new StreamWriter(fsOutput, Encoding.GetEncoding(949));

                using (var ms = new MemoryStream()) {
                    using (var cs = new CryptoStream(ms, encrypt, CryptoStreamMode.Write)) {
                        byte[] xXml = Encoding.GetEncoding(949).GetBytes(tmp라이센스);
                        cs.Write(xXml, 0, xXml.Length);
                    }
                    xBuff = ms.ToArray();
                }

                String Output = Convert.ToBase64String(xBuff);
                fsWrite.Write(Output);
                fsWrite.Flush();

                fsWrite.Close(); fsOutput.Close();
            }
            catch (IOException) {
                메세지 = string.Format("해당 라이센스파일을 생성 하는데 실패 했습니다. {0}", 구분);
                return;
            } catch (Exception e) {
                메세지 = string.Format("해당 라이센스파일을 생성 하는데 실패 했습니다. {0}\n{1}", 구분, e.Message);
                return;
            }

        }

        private enum CryptoAction
        {
            암호화 = 1,
            복호화 = 2
        }

        private void 진행바초기화(DevExpress.XtraBars.BarEditItem bar진행)
        {
            RepositoryItemProgressBar 진행상황바 = bar진행.Edit as RepositoryItemProgressBar;
            진행상황바.ShowTitle = true;    // 값표시
            진행상황바.PercentView = true;  // %로 표시 
            진행상황바.Maximum = 100;
            진행상황바.Minimum = 0;
            진행상황바.Step = 1;
            진행상황바.ProgressViewStyle = DevExpress.XtraEditors.Controls.ProgressViewStyle.Solid;

            bar진행.EditValue = 0;
        }

        private void event_메인화면_setMessage(string[] 전달인수)
        {
            switch (전달인수[0])
            {
                case "버튼활성화":
                    버튼활성화(전달인수[1]);
                    break;
                case "진행상황":
                    bar진행상황.EditValue = Convert.ToInt32(전달인수[1]);
                    break;
                case "소요시간종료":
                case "업로드소요시간":
                    소요시간Timer.Stop();
                    break;
                case "담보Merge소요시간":
                case "증권Merge소요시간":
                case "FTP소요시간":
                    시간표시메뉴 = 전달인수[1];
                    메세지 = 전달인수[2];
                    시작시간 = DateTime.Now;
                    소요시간Timer.Start();
                    break;
                case "담보Merge소요시간종료":
                case "증권Merge소요시간종료":
                case "FTP소요시간종료":
                    소요시간Timer.Stop();
                    메세지 = "작업종류";
                    break;


                case "증권약관완료":
                    메세지 = "증권,약관 작업완료";
                    반복Timer.Start();
                    break;
                case "담보찾기오류":
                    메세지 = "작업준비중";
                    bar메세지2.EditValue = 전달인수[1];
                    break;

                case "비교대상한글파일":
                    메세지 = "원본파일 : 대상파일 비교";
                    barHwp비교파일경로.EditValue = 전달인수[1];
                    break;
                case "정확도100미만":
                    bar메세지2.EditValue = "100%일치 하지 않는 담보존재";
                    break;
                case "분석중1":
                case "분석중2":
                    if (전달인수[0] == "분석중1") {
                        bar분석중1.EditValue = Convert.ToInt32(전달인수[1]);
                    } else if (전달인수[0] == "분석중2") {
                        bar분석중2.EditValue = Convert.ToInt32(전달인수[1]);
                    }
                    break;
                case "개정한글경로":
                    bar개정한글경로.EditValue = 전달인수[1];
                    if (전달인수[2] == ".HWP") {
                        bar개정한글수정경로.EditValue = 전달인수[1];
                    }
                    break;
                case "초안한글경로":
                    bar초안한글경로.EditValue = 전달인수[1];
                    if (전달인수[2] == ".HWP") {
                        bar편집한글수정경로.EditValue = 전달인수[1];
                    }
                    break;
                case "한글정보비교하기":
                    bar한글비교중.EditValue = Convert.ToInt32(전달인수[1]);
                    break;
                case "FTP진행":
                    barFTP현재진행.EditValue = Convert.ToInt32(전달인수[1]);
                    barFTP전체진행.EditValue = Convert.ToInt32(전달인수[2]);
                    barFTP표시진행.EditValue = 전달인수[3];
                    break;
                case "맞춤약관시작":
                    맞춤약관시작();
                    break;

            }
            bar메세지.EditValue = 메세지;
        }
        private void 버튼활성화(string 활성화인수)
        {
            switch (활성화인수.ToLower())
            {
                case "담보비교":
                    bar담보비교.Enabled = true;
                    if (bar자동.Checked && 약관형태 == "분리담보") bar담보비교_클릭(); // 자동화1
                    break;
                case "encode":
                    barEncode.Enabled = true;
                    if (bar자동.Checked && 약관형태 == "분리담보") barEncode_클릭(); // 자동화2
                    break;
                case "담보upload":
                    bar담보upload.Enabled = true;
                    if (bar자동.Checked && 약관형태 == "분리담보") bar담보upload_클릭(); // 자동화3
                    break;
                case "update":
                    barUpdate.Enabled = true;
                    break;
            }
        }

        private void 기본메뉴_SelectedPageChanged(object sender, EventArgs e)
        {
            기본메뉴제어();
        }
        private void 기본메뉴제어()
        {

            반복Timer.Stop();
            bar메세지.EditValue = "작업준비중";
            bar메세지2.EditValue = "작업준비중";

            진행바초기화(bar진행상황);
            진행바초기화(bar분석중1);
            진행바초기화(bar분석중2);
            진행바초기화(bar한글비교중);

            진행바초기화(barFTP현재진행);
            진행바초기화(barFTP전체진행);

            string 선택메뉴 = mnu기본메뉴.SelectedPage.Name;
            // 현재 로드되어 있는 폼을 닫는다.
            foreach (Control cont in Panel_Fill.Controls) {
                if (cont is Form) {
                    Form closeForm = new Form();
                    closeForm = (Form)cont;
                    closeForm.Close();
                    break;
                }
            }

            Form 현재폼 = new Form();
            switch (선택메뉴)
            {
                case "mnu담보Merge":
                    현재폼 = new frm담보Merge(파라메타);
                    break;

                case "mnu담보관리":
                    현재폼 = new frm담보관리(약관형태);
                    담보관리초기화();
                    break;

                case "mnu담보분리":
                    현재폼 = new frm담보분리();
                    break;

                case "mnu한글수정":
                    현재폼 = new frm한글수정();
                    break;

            }
            if (현재폼 != null) 작업폼제어(현재폼);
        }


        private void 작업폼제어(Form 현재폼)
        {
            경로설정불러오기(); // 입,출력경로 설정파일 읽기;

            현재폼.TopLevel = false;
            현재폼.FormBorderStyle = FormBorderStyle.None;
            현재폼.Dock = DockStyle.Fill;
            Panel_Fill.Controls.Add(현재폼);
            FormBorderStyle = FormBorderStyle.Sizable;

            barHwp경로.EditValue = "";
            barHwp비교파일경로.EditValue = "";
            bar유효성설정.Enabled = false;
            현재폼.Show();
        }

        private string 설정파일path = string.Format("{0}\\경로설정.txt", Application.StartupPath);
        private void 경로설정불러오기()
        {

            if (!new System.IO.FileInfo(설정파일path).Exists)
                return;
            string[] 경로 = System.IO.File.ReadAllLines(설정파일path, Encoding.GetEncoding(949));
            barinputpath.EditValue = 경로[0].Substring(2);
            baroutputpath.EditValue = 경로[1].Substring(2);
        }

        private void bar담보비교_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            bar담보비교_클릭();
        }
        private void bar담보비교_클릭()
        {
            담보관리event = new string[10];
            담보관리event[0] = "담보비교";
            담보관리event[1] = 약관형태;
            담보관리_SetMessage.Invoke(담보관리event);

            bar담보upload.Enabled = 약관형태 == "전체약관" ? true : false;
        }



        private void barEncode_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            barEncode_클릭();
        }
        private void barEncode_클릭()
        {
            진행바초기화(bar진행상황);
            진행바초기화(bar분석중1);
            진행바초기화(bar분석중2);
            진행바초기화(bar한글비교중);
            진행바초기화(barFTP현재진행);
            진행바초기화(barFTP전체진행);

            담보관리event = new string[10];
            담보관리event[0] = "Encode";
            담보관리_SetMessage.Invoke(담보관리event);

            시작시간 = DateTime.Now;
            소요시간Timer.Start();

            bar담보upload.Enabled = true;
            barUpdate.Enabled = true;
        }



        int a = 1;
        private void 반복Timer_Tick(object sender, EventArgs e)
        {
            a++;
            if (a % 2 == 1) {
                bar메세지.EditValue = "";
            } else {
                bar메세지.EditValue = "증권, 선택약관 매칭 오류";
            }

            bar메세지.Edit.Appearance.ForeColor = Color.Blue;

        }
        private void 소요시간Timer_Tick(object sender, EventArgs e)
        {

            DateTime 지금시간 = DateTime.Now;
            TimeSpan 시간차이 = 지금시간 - 시작시간;

            int 시 = 시간차이.Hours;
            int 분 = 시간차이.Minutes;
            int 초 = 시간차이.Seconds;
            string 소요시간 = string.Format("{0:00} : {1:00} : {2:00}", 시, 분, 초);
            switch (시간표시메뉴)
            {
                case "HwpMerge":
                    bar담보Merge소요시간.EditValue = 소요시간;
                    break;
                case "PDFMerge":
                    bar증권Merge소요시간.EditValue = 소요시간;
                    break;
                case "FTPUpload":
                    barFTP업로드소요시간.EditValue = 소요시간;
                    break;
                default:
                    bar소요시간.EditValue = 소요시간;
                    break;
            }
        }



        private void bar담보upload_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            bar담보upload_클릭();
        }
        private void bar담보upload_클릭()
        {
            시작시간 = DateTime.Now;
            소요시간Timer.Start();

            진행바초기화(bar진행상황);
            진행바초기화(bar분석중1);
            진행바초기화(bar분석중2);
            진행바초기화(bar한글비교중);
            진행바초기화(barFTP현재진행);
            진행바초기화(barFTP전체진행);
            담보관리event = new string[10];
            담보관리event[0] = "담보upload";
            담보관리event[1] = 약관형태;
            담보관리_SetMessage.Invoke(담보관리event);

            bar담보upload.Enabled = false;
        }

        private void bar작업시작_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {

            if (bar작업시작.Down == true) {

                if (string.IsNullOrEmpty(barinputpath.EditValue.ToString()) || string.IsNullOrEmpty(baroutputpath.EditValue.ToString())) {
                    MessageBox.Show("입/출력 경로를 입력하세요.");
                    return;
                }

                담보MergeEvent = new string[10];
                담보MergeEvent[0] = "작업시작";
                담보MergeEvent[1] = barinputpath.EditValue.ToString();
                담보MergeEvent[2] = baroutputpath.EditValue.ToString();

                담보MergeEvent[3] = ((bool)chk옵저버.EditValue == true) ? "1" : "0"; // 사용않함
                담보Merge_SetMessage.Invoke(담보MergeEvent);

            } else {
                기본메뉴제어();
            }

        }

        private void bar감시폴더저장_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            string[] 저장 = new string[] { "I:" + barinputpath.EditValue, "O:" + baroutputpath.EditValue };
            System.IO.File.WriteAllLines(설정파일path, 저장, System.Text.Encoding.GetEncoding(949));
            MessageBox.Show("입/출력 경로를 저장하였습니다.", "저장완료", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void bar입력파일Path_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            감시폴더설정.Title = "감시폴더가 시작할 Folder를 선택하세요";
            if (감시폴더설정.ShowDialog() == DialogResult.OK)
                barinputpath.EditValue = 감시폴더설정.SelectedPath + "\\";


        }

        private void bar출력파일Path_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            감시폴더설정.Title = "파일이 출력되는 Folder를 선택하세요.";
            if (감시폴더설정.ShowDialog() == DialogResult.OK)
                baroutputpath.EditValue = 감시폴더설정.SelectedPath + "\\";
        }


        private void mnu기본메뉴_DragDrop(object sender, DragEventArgs e)
        {
            string[] 한글파일 = (string[])e.Data.GetData(DataFormats.FileDrop, false);

            if (new System.IO.FileInfo(한글파일[0]).Extension.ToUpper() == ".HWP")
            {

                string 시트탭이름 = mnu기본메뉴.SelectedPage.Name;

                switch (시트탭이름)
                {
                    case "mnu담보분리":

                        barHwp경로.EditValue = 한글파일[0];

                        담보분리Event = new string[10];
                        담보분리Event[0] = "한글파일경로전달";
                        담보분리Event[1] = (string)barHwp경로.EditValue;
                        담보분리_SetMessage.Invoke(담보분리Event);
                        break;

                    case "mnu한글수정":

                        if (string.IsNullOrWhiteSpace((string)bar한글종류선택.EditValue)) {
                            MessageBox.Show("한글종류에서 개정한글 또는 편집한글을 선택하세요", "한글종류 선택(datazone천재소년)", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            return;
                        } else if ((string)bar한글종류선택.EditValue == "개정한글") {
                            bar개정한글수정경로.EditValue = 한글파일[0];
                        } else if ((string)bar한글종류선택.EditValue == "편집한글") {
                            bar편집한글수정경로.EditValue = 한글파일[0];
                        } else {
                            return;
                        }

                        한글수정Event = new string[10];
                        한글수정Event[0] = "편집HWP경로";
                        한글수정Event[1] = 한글파일[0];
                        한글수정Event[2] = (string)bar한글종류선택.EditValue;
                        한글수정_SetMessage.Invoke(한글수정Event);
                        break;
                }

            }

        }


        private void mnu기본메뉴_DragOver(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Link;
        }

        private void bar인덱스생성_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            if (string.IsNullOrEmpty((string)barHwp경로.EditValue))
            {
                MessageBox.Show("한글파일을 선택하여 주세요.", "파일선택오류", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            담보분리Event = new string[10];
            담보분리Event[0] = "인덱스생성";
            담보분리Event[1] = (string)barHwp경로.EditValue;
            담보분리_SetMessage.Invoke(담보분리Event);
        }


        private void bar인덱스저장_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            if (string.IsNullOrEmpty((string)barHwp경로.EditValue))
            {
                MessageBox.Show("한글파일을 선택하여 주세요.", "파일선택오류", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            담보분리Event = new string[10];
            담보분리Event[0] = "인덱스저장";
            담보분리_SetMessage.Invoke(담보분리Event);
        }

        private void barHwp파일선택_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            string 구분 = "";
            Hwp파일불러오기(구분);
        }

        private void bar담보쪼개기_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            if (string.IsNullOrEmpty((string)barHwp경로.EditValue)) {
                MessageBox.Show("한글파일을 선택하여 주세요.", "파일선택오류", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            담보분리Event = new string[10];
            담보분리Event[0] = "담보쪼개기";
            담보분리Event[1] = (string)barHwp경로.EditValue;
            담보분리_SetMessage.Invoke(담보분리Event);
        }



        private void 고객사선택(out string 고객사코드, int 호출인수)
        {
            고객사코드 = "";
            if ((string)cob고객사.EditValue == "고객사선택") {
                MessageBox.Show("검색 할 고객사를 선택하세요", "입력오류", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            } else {
                switch (cob고객사.EditValue) {
                    case "메리츠화재":
                        고객사코드 = "MZ";
                        break;
                    case "DB손해보험":
                        고객사코드 = "DB";
                        break;
                    case "DB손해보험(우)":
                        고객사코드 = "D2";
                        break;
                    case "KB손해보험":
                        고객사코드 = "KB";
                        break;
                    case "MG손해보험":
                        고객사코드 = "MG";
                        break;
                    case "AIG손해보험":
                        고객사코드 = "AI";
                        break;
                    case "처브화재":
                        고객사코드 = "CH";
                        break;
                    case "롯데손해보험":
                        고객사코드 = "LT";
                        break;
                    case "데이타존":
                        고객사코드 = "DZ";
                        break;

                    default:
                        고객사코드 = "오류";
                        MessageBox.Show("검색코드 오류", "입력오류", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                }
            }

        }

        private void barUpdate_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            
            string 고객사코드 = "";
            고객사선택(out 고객사코드, 1);
            if (고객사코드 == "오류")
                return;

            담보관리event = new string[10];
            담보관리event[0] = "담보update";
            담보관리event[1] = 고객사코드;
            담보관리event[2] = 테이블명;
            담보관리event[3] = 약관형태;
            담보관리_SetMessage.Invoke(담보관리event);
        }
    

        private void bar담보유효성검사_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            담보분리Event = new string[10];
            담보분리Event[0] = "담보합치기";
            담보분리Event[1] = (string)barHwp경로.EditValue;
            담보분리_SetMessage.Invoke(담보분리Event);
        }

        private void barHwp비교파일_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            string 구분 = "비교";
            Hwp파일불러오기(구분);
        }

        private void Hwp파일불러오기(string 구분)
        {
            string 타이틀문구 = "";
            string 파일이름문구 = "";

            switch (구분)
            {
                case "비교":
                    타이틀문구 = "비교할 Hwp파일을 선택";
                    파일이름문구 = "비교할 Hwp파일을 선택 하세요.";
                    break;

                case "편집":
                    타이틀문구 = "편집할 Hwp파일을 선택";
                    파일이름문구 = "편집할 Hwp파일을 선택 하세요.";
                    break;

                case "유효성설정":
                case "":
                    타이틀문구 = "특별담보 Hwp파일을 선택";
                    파일이름문구 = "특별담보 Hwp파일을 선택 하세요.";
                    break;
            }

            Hwp파일선택.Title = 타이틀문구;
            Hwp파일선택.FileName = 파일이름문구;
            Hwp파일선택.Filter = "아래한글 파일 (*.hwp) | *.hwp; | 모든 파일 (*.*) | *.*";
            Hwp파일선택.FilterIndex = 1;
            Hwp파일선택.RestoreDirectory = true;

            if (Hwp파일선택.ShowDialog() == DialogResult.OK) {

                switch (구분) {
                    case "비교할HWP경로":
                    case "한글파일경로전달":
                    case "":

                        barHwp비교파일경로.EditValue = Hwp파일선택.FileName;
                        담보분리Event = new string[10];
                        담보분리Event[0] = string.IsNullOrWhiteSpace(구분) ? "한글파일경로전달" : 구분;     // 공백이면
                        담보분리Event[1] = Hwp파일선택.FileName;
                        if (구분 == "한글파일경로전달") {
                            담보분리Event[2] = "유효성설정";        // '유효성설정' 호출시 값전달.
                        }

                        담보분리_SetMessage.Invoke(담보분리Event);
                        break;

                    case "편집":

                        bar개정한글수정경로.EditValue = Hwp파일선택.FileName;
                        한글수정Event = new string[10];
                        한글수정Event[0] = "편집HWP경로";
                        한글수정Event[1] = Hwp파일선택.FileName;
                        한글수정_SetMessage.Invoke(한글수정Event);
                        break;

                }
            }
        }

        private void barHwp비교파일경로_EditValueChanged(object sender, EventArgs e)
        {
            if (barHwp비교파일경로.EditValue.ToString().Trim() != "")
                bar유효성설정.Enabled = true;
        }

        private void bar유효성설정_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            if (barHwp경로.EditValue.ToString() == "") {
                var 확인 = MessageBox.Show("비교할 원본 한글파일을 선택 하시겠습니까?", "원본파일선택", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (확인 == DialogResult.Yes) {
                    string 구분 = "유효성설정";
                    Hwp파일불러오기(구분);
                } else {
                    return;
                }

            }
            if (barHwp비교파일경로.EditValue.ToString() == "") {
                var 확인 = MessageBox.Show("비교할 한글파일을 선택 하시겠습니까?", "비교한글파일선택", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (확인 == DialogResult.Yes) {
                    string 구분 = "비교";
                    Hwp파일불러오기(구분);
                } else {
                    return;
                }
            }

            if (barHwp비교파일경로.EditValue.ToString() != "" && barHwp비교파일경로.EditValue.ToString() != "")
            {
                담보분리Event = new string[10];
                담보분리Event[0] = "한글유효성설정";
                담보분리_SetMessage.Invoke(담보분리Event);
            }


        }

        private void bar유효성검사_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            담보분리Event = new string[10];
            담보분리Event[0] = "한글유효성검사";
            담보분리_SetMessage.Invoke(담보분리Event);

        }

        private void bar스타일정의_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            System.Diagnostics.Process.Start("Notepad.exe", string.Format("{0}\\스타일.txt", Application.StartupPath));
        }

        private void bar문단부호_CheckedChanged(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            담보분리Event = new string[10];
            담보분리Event[0] = "문단부호표시";
            담보분리_SetMessage.Invoke(담보분리Event);
        }

        private void barHwp분석_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            if (string.IsNullOrEmpty((string)barHwp경로.EditValue)) {
                MessageBox.Show("한글파일을 선택하여 주세요.", "파일선택오류", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            담보분리Event = new string[10];
            담보분리Event[0] = "Hwp분석";
            담보분리Event[1] = (string)barHwp경로.EditValue;
            담보분리_SetMessage.Invoke(담보분리Event);
        }

        private void bar편집한글_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            string 구분 = "편집";
            Hwp파일불러오기(구분);
        }

        private void bar한글정보저장_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            한글수정Event = new string[10];
            한글수정Event[0] = "한글정보저장";
            한글수정Event[1] = (string)bar개정한글경로.EditValue ?? "";   // ??는 왼쪽이 Null이면 오른쪽 Null이 아니면 왼쪽
            한글수정Event[2] = (string)bar초안한글경로.EditValue ?? "";

            한글수정_SetMessage.Invoke(한글수정Event);
        }

        private void bar한글정보비교_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            한글수정Event = new string[10];
            한글수정Event[0] = "한글정보비교";
            한글수정_SetMessage.Invoke(한글수정Event);
        }

        private void bar한글화면크기_CheckedChanged(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            한글수정Event = new string[10];
            한글수정Event[0] = "한글화면크기";
            if (bar한글화면크기.Checked == true) {
                한글수정Event[1] = "확대";
            } else {
                한글수정Event[1] = "축소";
            }

            한글수정_SetMessage.Invoke(한글수정Event);

        }

        private void bar수정_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {

            if (string.IsNullOrWhiteSpace((string)bar개정한글경로.EditValue)) {
                MessageBox.Show("개정한글경로가 공백입니다.", "개정한글경로오류(datazone천재소년)", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (string.IsNullOrWhiteSpace((string)bar초안한글경로.EditValue)) {
                MessageBox.Show("초한한글경로가 공백입니다.", "초안한글경로오류(datazone천재소년)", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (string.IsNullOrWhiteSpace((string)bar개정한글수정경로.EditValue)) {
                MessageBox.Show("개정한글이 없습니다.", "개정한글오류(datazone천재소년)", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (string.IsNullOrWhiteSpace((string)bar편집한글수정경로.EditValue)) {
                MessageBox.Show("편집한글이 없습니다.", "편집한글오류(datazone천재소년)", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            한글수정Event = new string[10];
            한글수정Event[0] = "한글수정";
            한글수정Event[1] = (string)bar편집한글수정경로.EditValue;
            한글수정_SetMessage.Invoke(한글수정Event);
        }

        private void bar스타일찾기_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {

            한글수정Event = new string[10];
            한글수정Event[0] = "스타일찾기";

            if (string.IsNullOrWhiteSpace((string)bar스타일선택.EditValue)) {
                MessageBox.Show("개정,편집한글에 대한 스타일을 선택하세요", "작업종류선택(datazone천재소년)", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            } else if ((string)bar스타일선택.EditValue == "개정스타일" &&
                       !string.IsNullOrWhiteSpace((string)bar개정스타일.EditValue) &&
                       !string.IsNullOrWhiteSpace((string)bar개정한글수정경로.EditValue)) {
                한글수정Event[1] = (string)bar스타일선택.EditValue;
                한글수정Event[2] = (string)bar개정스타일.EditValue;
                한글수정Event[3] = (string)bar개정한글수정경로.EditValue;

            } else if ((string)bar스타일선택.EditValue == "편집스타일" &&
                       !string.IsNullOrWhiteSpace((string)bar편집스타일.EditValue) &&
                       !string.IsNullOrWhiteSpace((string)bar편집한글수정경로.EditValue)) {
                한글수정Event[1] = (string)bar스타일선택.EditValue;
                한글수정Event[2] = (string)bar편집스타일.EditValue;
                한글수정Event[3] = (string)bar편집한글수정경로.EditValue;

            }
            else {
                MessageBox.Show("개정스타일 이면 개정스타일값, 개정한글을 \n편집스타일 이면 편집스타일값, 편집한글을 입력하세요.", "스타일오류(datazone천재소년)", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            한글수정_SetMessage.Invoke(한글수정Event);

        }
        private void barSearch_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            // 공통 함수를 통해 유효성 검사 통과 및 변수 할당
            if (!공통검색조건확인(out string 고객사코드, out string 상품코드, out string 개정일, out string 분류코드, out string 담보코드))
                return;

            bar메세지3.EditValue = "해당담보를 더블클릭하면 미리보기 가능";

            담보관리event = new string[10]
            {
                "담보검색",                                // [0]
                고객사코드,                                // [1]
                상품코드,                                  // [2]
                개정일,                                    // [3]
                분류코드,                                  // [4] (값이 없으면 "" 전달)
                담보코드,                                  // [5] (값이 없으면 "" 전달)
                약관형태,                                  // [6] (전역 변수)
                테이블명,                                  // [7] (전역 변수)
                null,                                      // [8] 
                null                                       // [9] 
            };

            담보관리_SetMessage.Invoke(담보관리event);

            bar담보upload.Enabled = false;
            bar상품삭제.Enabled = true; // 검색 성공 시에만 삭제버튼 활성화
            barUpdate.Enabled = true;
        }

        private void bar상품삭제_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            // 공통 함수로 검색과 100% 동일한 기준의 변수를 가져옴
            if (!공통검색조건확인(out string 고객사코드, out string 상품코드, out string 개정일, out string 분류코드, out string 담보코드))
                return;

            // 삭제 확인 알림창 구성 (동적으로 선택된 항목만 표기)
            string msg = $"다음 조건으로 검색된 데이터를 정말 삭제하시겠습니까?\n\n" +
                         $"[삭제 대상 정보]\n" +
                         $"- 상품코드: {상품코드}\n" +
                         $"- 개정일: {개정일}";

            if (!string.IsNullOrEmpty(분류코드))
                msg += $"\n- 담보분류: {분류코드}";
            if (약관형태 == "분리담보" && !string.IsNullOrEmpty(담보코드))
                msg += $"\n- 담보코드: {담보코드}";

            if (MessageBox.Show(msg, "삭제 확인", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.No)
            {
                return;
            }

            try
            {
                using (SqlConnection AdoNet = new SqlConnection(약관DB))
                {
                    AdoNet.Open();

                    // =========================================================================
                    //  삭제 쿼리 동적 생성 
                    // =========================================================================

                    // 필수 조건
                    string sqlDelete = $"DELETE FROM {테이블명} WHERE 상품코드 = @상품코드 AND 개정일 = @개정일";

                    // 1. 담보분류 조건 (값이 있을 때만 추가)
                    if (!string.IsNullOrEmpty(분류코드))
                    {
                        sqlDelete += " AND 담보분류 = @담보분류";
                    }

                    // 2. 담보코드 조건 (★분리담보 형태이면서 값이 있을 때만 추가★)
                    if (약관형태 == "분리담보" && !string.IsNullOrEmpty(담보코드))
                    {
                        sqlDelete += " AND 담보코드 = @담보코드";
                    }

                    using (SqlCommand cmdDelete = new SqlCommand(sqlDelete, AdoNet))
                    {
                        // 파라미터 매핑
                        cmdDelete.Parameters.AddWithValue("@상품코드", 상품코드);
                        cmdDelete.Parameters.AddWithValue("@개정일", 개정일);

                        if (!string.IsNullOrEmpty(분류코드))
                            cmdDelete.Parameters.AddWithValue("@담보분류", 분류코드);

                        if (약관형태 == "분리담보" && !string.IsNullOrEmpty(담보코드))
                            cmdDelete.Parameters.AddWithValue("@담보코드", 담보코드);

                        int deletedCount = cmdDelete.ExecuteNonQuery();
                        MessageBox.Show($"총 {deletedCount}건의 데이터가 삭제되었습니다.", "삭제 완료", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }

                // 삭제 완료 후 화면 초기화
                bar초기화_ItemClick(null, null);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"삭제 중 오류가 발생했습니다:\n{ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // [공통 함수] 검색 및 삭제 시 공통으로 사용하는 유효성 검사 및 변수 추출
        private bool 공통검색조건확인(out string 고객사코드, out string 상품코드, out string 개정일, out string 분류코드, out string 담보코드)
        {
            고객사코드 = ""; 상품코드 = ""; 개정일 = ""; 분류코드 = ""; 담보코드 = "";

            // 1. 고객사 유효성 검사
            string selectedCustomer = cob고객사.EditValue?.ToString();
            if (string.IsNullOrEmpty(selectedCustomer) || selectedCustomer == "고객사선택")
            {
                MessageBox.Show("고객사를 선택하세요.", "입력오류", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }

            고객사선택(out 고객사코드, 2);
            if (고객사코드 == "오류") return false;

            // 2. 상품코드 유효성 검사 (필수)
            상품코드 = cob상품코드.EditValue?.ToString();
            if (string.IsNullOrEmpty(상품코드))
            {
                MessageBox.Show("상품코드를 선택하세요.", "입력오류", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }

            // 3. 개정일 유효성 검사 (필수)
            개정일 = cob개정일.EditValue?.ToString() ?? "";
            if (string.IsNullOrEmpty(개정일))
            {
                MessageBox.Show("개정일을 선택하세요.", "입력오류", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }

            // 4. 분류 & 담보 (값이 없을 수도 있으므로 null을 빈 문자열로 처리)
            분류코드 = cob분류코드.EditValue?.ToString() ?? "";
            담보코드 = cob담보코드.EditValue?.ToString() ?? "";

            return true;
        }


        private void bar약관형태_EditValueChanged(object sender, EventArgs e)
        {
            약관형태 = (string)bar약관형태.EditValue;

            담보관리event = new string[10];
            담보관리event[0] = "약관형태";
            담보관리event[1] = 약관형태;
            담보관리_SetMessage.Invoke(담보관리event);
            
            if (약관형태 == "분리담보")
            {
                bar담보비교.Caption = "담보비교";
                bar담보upload.Caption = "담보upload";
                cob담보코드.Enabled = true;
            }
            else if (약관형태 == "전체약관")
            {
                bar담보비교.Caption = "(통)약관비교";
                bar담보upload.Caption = "(통)약관upload";
                cob담보코드.Enabled = false;
            }
            
            고객사초기화();

            if ((string)cob고객사.EditValue != "고객사선택")
            {
                edit고객사_EditValueChanged(null, null);
            }
        }

        private DevExpress.XtraBars.BarEditItem cobBoxitem초기화(DevExpress.XtraBars.BarEditItem cob객체)
        {
            RepositoryItemComboBox comboBoxItem = cob객체.Edit as RepositoryItemComboBox;
            comboBoxItem.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
            comboBoxItem.Items.Clear();

            cob객체.EditValue = "";
            return cob객체;


        }

        private void 고객사초기화()
        {
            RepositoryItemComboBox comboBoxItem = cob고객사.Edit as RepositoryItemComboBox;
            comboBoxItem.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
            comboBoxItem.Items.Clear();

            comboBoxItem.Items.Add("메리츠화재");
            comboBoxItem.Items.Add("DB손해보험");
            comboBoxItem.Items.Add("DB손해보험(우)");
            comboBoxItem.Items.Add("KB손해보험");
            comboBoxItem.Items.Add("AIG손해보험");
            comboBoxItem.Items.Add("처브화재");
            comboBoxItem.Items.Add("롯데손해보험");
            comboBoxItem.Items.Add("MG손해보험");
            comboBoxItem.Items.Add("데이타존");

            cob고객사.EditValue = "고객사선택";
            개정년초기화();
        }

        private void 개정년초기화()
        {
            cob개정년 = cobBoxitem초기화(cob개정년);
            상품코드초기화();
        }

        private void 상품코드초기화()
        {
            cob상품코드 = cobBoxitem초기화(cob상품코드);
            개정일초기화();
            분류코드초기화();
            담보코드초기화();
        }

        private void 개정일초기화()
        {
            cob개정일 = cobBoxitem초기화(cob개정일);

        }

        private void 분류코드초기화()
        {
            cob분류코드 = cobBoxitem초기화(cob분류코드);

        }

        private void 담보코드초기화()
        {
            cob담보코드 = cobBoxitem초기화(cob담보코드);
        }


        private string cob고객사코드 = "";
        private string 테이블명 = "";

        private void edit고객사_EditValueChanged(object sender, EventArgs e)
        {
            string selectedValue = cob고객사.EditValue?.ToString();

            // 1. 유효성 검사 및 초기화
            if (string.IsNullOrEmpty(selectedValue) || selectedValue == "고객사선택") return;

            고객사선택(out cob고객사코드, 3);
            if (cob고객사코드 == "오류") return;

            개정년초기화();

            if (cob개정년.Edit is RepositoryItemComboBox comboBoxItem)
            {
                comboBoxItem.BeginUpdate(); // UI 업데이트 성능 향상
                try
                {
                    comboBoxItem.Items.Clear();
                    comboBoxItem.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;

                    using (SqlConnection conn = new SqlConnection(약관DB))
                    {
                        conn.Open();
                        string sqlStr = "";

                        if (약관형태 == "분리담보")
                        {
                            // LIKE 검색 시 보안을 위해 매개변수 사용
                            sqlStr = "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME LIKE @TableName ORDER BY TABLE_NAME DESC";
                            using (SqlCommand cmd = new SqlCommand(sqlStr, conn))
                            {
                                cmd.Parameters.AddWithValue("@TableName", $"%담보{cob고객사코드}%");
                                using (SqlDataReader reader = cmd.ExecuteReader())
                                {
                                    while (reader.Read())
                                    {
                                        string tableName = reader["TABLE_NAME"].ToString();
                                        string[] parts = tableName.Split('_');
                                        string year = "";

                                        if (parts.Length == 1) // 예: 담보CH
                                            year = DateTime.Now.Year.ToString();
                                        else if (parts.Length == 2 && parts[1] != "원본") // 예: 담보CH_21
                                            year = $"20{parts[1]}";

                                        if (!string.IsNullOrEmpty(year) && !comboBoxItem.Items.Contains(year))
                                            comboBoxItem.Items.Add(year);
                                    }
                                }
                            }
                        }
                        else if (약관형태 == "전체약관")
                        {
                            // 테이블명 자체는 매개변수화가 불가능하므로 화이트리스트 검증 권장 (여기서는 기존 로직 유지)
                            sqlStr = $@"SELECT DISTINCT LEFT(개정일, 2) AS 개정년 
                                FROM 약관_{cob고객사코드} 
                                ORDER BY 개정년 DESC";

                            using (SqlCommand cmd = new SqlCommand(sqlStr, conn))
                            {
                                using (SqlDataReader reader = cmd.ExecuteReader())
                                {
                                    while (reader.Read())
                                    {
                                        comboBoxItem.Items.Add($"20{reader["개정년"]}");
                                    }
                                }
                            }
                        }
                    }
                    //정렬
                    var sortedItems = comboBoxItem.Items.Cast<string>().OrderByDescending(x => x).ToArray();
                    comboBoxItem.Items.Clear();
                    comboBoxItem.Items.AddRange(sortedItems);
                }
                finally
                {
                    comboBoxItem.EndUpdate(); // 업데이트 종료
                }
            }
        }
        
        private void cob개정년_EditValueChanged(object sender, EventArgs e)
        {

            string 개정년 = cob개정년.EditValue?.ToString();
            if (string.IsNullOrEmpty(개정년) || 개정년.Length < 4)
            {
                return;
            }

            상품코드초기화();

            RepositoryItemComboBox comboBoxItem = cob상품코드.Edit as RepositoryItemComboBox;
            if (comboBoxItem == null) return;

            // 대량의 데이터 추가 시 화면 깜빡임 방지
            comboBoxItem.BeginUpdate();

            try
            {
                comboBoxItem.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
                comboBoxItem.Items.Clear();

                string SqlStr = "";
                string 년도끝자리 = 개정년.Substring(2, 2);

                using (SqlConnection AdoNet = new SqlConnection(약관DB))
                {
                    AdoNet.Open();

                    using (SqlCommand sqlcmd = new SqlCommand())
                    {
                        sqlcmd.Connection = AdoNet;

                        if (약관형태 == "분리담보")
                        {
                            string 현재년도 = DateTime.Now.ToString("yyyy");

                            if (현재년도 != 개정년)
                                테이블명 = string.Format("담보{0}_{1}", cob고객사코드, 년도끝자리);
                            else
                                테이블명 = string.Format("담보{0}", cob고객사코드);

                            SqlStr = string.Format("SELECT 상품코드 FROM {0} GROUP BY 상품코드 ORDER BY 상품코드", 테이블명);
                            sqlcmd.CommandText = SqlStr;
                        }
                        else if (약관형태 == "전체약관")
                        {
                            테이블명 = $"약관_{cob고객사코드}";
                            // SQL Injection 방지를 위해 파라미터 사용
                            SqlStr = $@"SELECT DISTINCT 상품코드 
                                FROM {테이블명} 
                                WHERE LEFT(개정일,2) = @년도끝자리 
                                ORDER BY 상품코드";
                            sqlcmd.CommandText = SqlStr;
                            sqlcmd.Parameters.AddWithValue("@년도끝자리", 년도끝자리);
                        }

                        // 쿼리가 비어있지 않을 때만 실행
                        if (!string.IsNullOrEmpty(SqlStr))
                        {
                            // reader 객체도 using으로 묶어 메모리 누수 방지
                            using (SqlDataReader reader = sqlcmd.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    comboBoxItem.Items.Add(reader["상품코드"].ToString());
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"상품코드 데이터 로드 중 오류 발생: {ex.Message}");
            }
            finally
            {
                // 에러가 나더라도 UI 업데이트 잠금 해제
                comboBoxItem.EndUpdate();
            }


        }

        private void cob상품코드_EditValueChanged(object sender, EventArgs e)
        {

            string tmp상품코드 = cob상품코드.EditValue?.ToString();
            if (string.IsNullOrEmpty(tmp상품코드))
            {
                return;
            }

            barSearch.Enabled = true;

            개정일초기화();
            분류코드초기화();
            담보코드초기화();

            // 분류코드는 여기서 데이터를 구하지 않으므로 기존 데이터 초기화(Clear)만 해줍니다.
            RepositoryItemComboBox comboBoxItem = cob분류코드.Edit as RepositoryItemComboBox;
            if (comboBoxItem != null)
            {
                comboBoxItem.BeginUpdate();
                comboBoxItem.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
                comboBoxItem.Items.Clear();
                comboBoxItem.EndUpdate();
            }

            // 2. 개정일 콤보박스 세팅
            RepositoryItemComboBox comboBoxItem1 = cob개정일.Edit as RepositoryItemComboBox;
            if (comboBoxItem1 == null) return;

            comboBoxItem1.BeginUpdate();
            try
            {
                comboBoxItem1.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
                comboBoxItem1.Items.Clear();

                // 3. DB 연결 및 개정일만 조회
                using (SqlConnection AdoNet = new SqlConnection(약관DB))
                {
                    AdoNet.Open();
                    string SqlStr = "";

                    if (약관형태 == "분리담보")
                    {
                        // 담보분류 조회 로직은 삭제하고, 바로 개정일 쿼리를 준비합니다.
                        SqlStr = $"SELECT 개정일 FROM {테이블명} WHERE 상품코드 = @tmp상품코드 GROUP BY 개정일 ORDER BY 개정일";
                    }
                    else if (약관형태 == "전체약관")
                    {
                        SqlStr = $"SELECT DISTINCT 개정일 FROM 약관_{cob고객사코드} WHERE 상품코드 = @tmp상품코드 ORDER BY 개정일";
                    }

                    // 쿼리가 정상적으로 준비되었을 때만 실행
                    if (!string.IsNullOrEmpty(SqlStr))
                    {
                        using (SqlCommand sqlcmd = new SqlCommand(SqlStr, AdoNet))
                        {
                            // SQL Injection 방지용 파라미터 적용
                            sqlcmd.Parameters.AddWithValue("@tmp상품코드", tmp상품코드);
                            using (SqlDataReader reader = sqlcmd.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    comboBoxItem1.Items.Add(reader["개정일"].ToString());
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"개정일 데이터 로드 중 오류 발생: {ex.Message}");
            }
            finally
            {
                // 안전하게 화면 업데이트 잠금 해제
                comboBoxItem1.EndUpdate();
            }

        }

        private void cob개정일_EditValueChanged(object sender, EventArgs e)
        {
            // 1. 상품코드와 개정일이 모두 정상적으로 선택되었는지 확인
            string tmp상품코드 = cob상품코드.EditValue?.ToString();
            string tmp개정일 = cob개정일.EditValue?.ToString();

            if (string.IsNullOrEmpty(tmp상품코드) || string.IsNullOrEmpty(tmp개정일))
            {
                return;
            }

            // 하위 단계인 분류코드와 담보코드를 초기화
            분류코드초기화();
            담보코드초기화();

            // 2. 분류코드 콤보박스 세팅
            RepositoryItemComboBox comboBoxItem = cob분류코드.Edit as RepositoryItemComboBox;
            if (comboBoxItem == null) return;

            comboBoxItem.BeginUpdate(); // UI 깜빡임 방지
            try
            {
                comboBoxItem.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
                comboBoxItem.Items.Clear();

                // 3. DB 연결 및 담보분류 조회
                using (SqlConnection AdoNet = new SqlConnection(약관DB))
                {
                    AdoNet.Open();
                    string SqlStr = "";

                    if (약관형태 == "분리담보")
                    {
                        // 상품코드와 개정일을 모두 조건(WHERE)으로 걸어 정확한 담보분류를 가져옵니다.
                        SqlStr = $"SELECT 담보분류 FROM {테이블명} WHERE 상품코드 = @tmp상품코드 AND 개정일 = @tmp개정일 GROUP BY 담보분류 ORDER BY 담보분류";
                    }
                    else if (약관형태 == "전체약관")
                    {
                        // 전체약관의 경우 테이블명 규칙에 맞게 쿼리 구성
                        SqlStr = $"SELECT DISTINCT 담보분류 FROM 약관_{cob고객사코드} WHERE 상품코드 = @tmp상품코드 AND 개정일 = @tmp개정일 GROUP BY 담보분류 ORDER BY 담보분류";
                    }

                    // 쿼리가 준비되었을 때만 실행
                    if (!string.IsNullOrEmpty(SqlStr))
                    {
                        using (SqlCommand sqlcmd = new SqlCommand(SqlStr, AdoNet))
                        {
                            // 파라미터를 추가하여 보안과 안정성 확보
                            sqlcmd.Parameters.AddWithValue("@tmp상품코드", tmp상품코드);
                            sqlcmd.Parameters.AddWithValue("@tmp개정일", tmp개정일);

                            using (SqlDataReader reader = sqlcmd.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    comboBoxItem.Items.Add(reader["담보분류"].ToString());
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"분류코드 로드 중 오류 발생: {ex.Message}");
            }
            finally
            {
                // 안전하게 화면 업데이트 잠금 해제
                comboBoxItem.EndUpdate();
            }
        }

        private void cob분류코드_EditValueChanged(object sender, EventArgs e)
        {

            if (약관형태 == "전체약관") return;

            // 1. 안전한 null 체크 (프로그램 강제 종료 방지)
            string tmp상품코드 = cob상품코드.EditValue?.ToString();
            string tmp분류코드 = cob분류코드.EditValue?.ToString();

            if (string.IsNullOrWhiteSpace(tmp상품코드) || string.IsNullOrWhiteSpace(tmp분류코드))
            {
                return;
            }

            담보코드초기화();

            RepositoryItemComboBox comboBoxItem = cob담보코드.Edit as RepositoryItemComboBox;
            if (comboBoxItem == null) return;

            // 2. 대량 데이터 처리 시 UI 깜빡임 및 멈춤 방지
            comboBoxItem.BeginUpdate();
            try
            {
                comboBoxItem.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
                comboBoxItem.Items.Clear();

                // 3. DB 연결 및 자원 자동 해제
                using (SqlConnection AdoNet = new SqlConnection(약관DB))
                {
                    AdoNet.Open();

                    // 테이블명은 문자열 보간법($)으로 넣고, 조건 값들은 파라미터(@)로 처리
                    string SqlStr = $"SELECT 담보코드 FROM {테이블명} WHERE 상품코드 = @상품코드 AND 담보분류 = @담보분류 ORDER BY 담보코드";

                    using (SqlCommand sqlcmd = new SqlCommand(SqlStr, AdoNet))
                    {
                        // 4. SQL Injection 해킹 방지 및 특수문자 오류 방지
                        sqlcmd.Parameters.AddWithValue("@상품코드", tmp상품코드);
                        sqlcmd.Parameters.AddWithValue("@담보분류", tmp분류코드);

                        // 리더 객체도 using으로 묶어 메모리 누수 방지
                        using (SqlDataReader reader = sqlcmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                comboBoxItem.Items.Add(reader["담보코드"].ToString());
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"담보코드 로드 중 오류 발생: {ex.Message}");
            }
            finally
            {
                // 5. 에러가 나더라도 반드시 화면 업데이트 잠금을 해제
                comboBoxItem.EndUpdate();
            }

        }

        private void bar약관업로드_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            담보MergeEvent = new string[10];
            담보MergeEvent[0] = "전체약관";
            담보MergeEvent[1] = barinputpath.EditValue.ToString();
            담보MergeEvent[2] = baroutputpath.EditValue.ToString();

            담보Merge_SetMessage.Invoke(담보MergeEvent);
        }

        private void bar초기화_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            담보관리초기화();
            담보관리event = new string[10];
            담보관리event[0] = "초기화";
            담보관리event[1] = 약관형태;
            담보관리_SetMessage.Invoke(담보관리event);
        }

        private void 담보관리초기화()
        {
            if( chkALL.Checked)
                고객사초기화();

            bar담보upload.Enabled = false;
            bar담보비교.Enabled = false;
            barEncode.Enabled = false;
            barUpdate.Enabled = false;
            barSearch.Enabled = false ;
            bar자동.Checked = false;
            bar상품삭제.Enabled = false;
            bar자동메세지.EditValue = "자동해제";
        }

        private void bar자동_CheckedChanged(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            if (bar자동.Checked)
                bar자동메세지.EditValue = "자동설정";
            else
                bar자동메세지.EditValue = "자동해제";
        }

        
    }

    public class PreventSleep
    {
        // 윈도우 API 불러오기
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern uint SetThreadExecutionState(uint esFlags);

        // 플래그 상수 정의
        private const uint ES_CONTINUOUS = 0x80000000;
        private const uint ES_SYSTEM_REQUIRED = 0x00000001; // 시스템 잠김 방지
        private const uint ES_DISPLAY_REQUIRED = 0x00000002; // 화면 꺼짐 방지

        /// <summary>
        /// 절전 모드 및 화면 보호기 실행을 막습니다. (작업 시작 시 호출)
        /// </summary>
        public static void DonSleep()
        {
            // 시스템이 잠기거나 화면이 꺼지는 것을 막음
            // HWP 같은 GUI 자동화는 화면이 꺼지면 에러가 날 수 있으므로 Display도 켜둬야 함
            SetThreadExecutionState(ES_CONTINUOUS | ES_SYSTEM_REQUIRED | ES_DISPLAY_REQUIRED);
        }

        /// <summary>
        /// 다시 절전 모드가 가능하도록 설정을 해제합니다. (프로그램 종료 시 호출)
        /// </summary>
        public static void AllowSleep()
        {
            SetThreadExecutionState(ES_CONTINUOUS);
        }
    }
}
