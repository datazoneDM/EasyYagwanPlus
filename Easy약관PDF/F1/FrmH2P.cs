using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace p1
{
    public partial class frmh2p : DevExpress.XtraEditors.XtraForm
    {
        private int currentHwpPID = 0;
        private dynamic hwp = null;

        public PictureBox[] imgHwp;
        public PictureBox[] imgPdf;
        private FileStream Logfs;
        private StreamWriter Logwr;
        private string 로그내용;
        private int _currentLogDay = -1;
        private string 시작폴더 = "";
        private string 파라메타 = "";
        private string _pidFilePath = "";

        public frmh2p(string[] args)
        {
            InitializeComponent();
            if (args.Length >= 1)
                파라메타 = args[0];
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            this.Visible = false;         // 처음에는 안 보이게
            this.DoubleBuffered = true;   // 깜빡임 최소화
            this.Opacity = 0;  
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            this.SuspendLayout();


            //시작폴더 = @"F1";   // 수정 1
            시작폴더 = new DirectoryInfo(Application.StartupPath).Name; // F1 ~ F8
            저장.개별폴더 = 시작폴더.ToUpper().Replace("F", ""); // 1 ~ 8             
            기본화면();
            일하는순서();

            this.ResumeLayout(false);
            this.PerformLayout();

            // 폼을 화면에 표시 (모든 준비 끝난 후)
            this.Visible = true;
            this.Refresh();  // 강제로 전체 리프레시 (한 번에 그림)
        }


        private void frmh2p_Load(object sender, EventArgs e)
        {
            // 프로그램이 켜져 있는 동안 절대 잠들지 않게 설정
            PreventSleep.DonSleep();
        }

        private async void 일하는순서()
        {
            //디폴트 저장 인수값
            저장.DB설정파일path = @"c:\Easy약관Plus\program\localDB.txt";
            저장.로그경로 = @"c:\Easy약관Plus\program\LogFile\";
            저장.기본출력폴더 = @"d:\Easy약관Plus\Output\";
            _pidFilePath = string.Format("{0}PID\\F{1}.PID", 저장.기본출력폴더, 저장.개별폴더);

            string 임시컴 = SystemInformation.ComputerName.ToLower();
            if (파라메타 == "앱실행_싱글처리")
            {
                if (임시컴.StartsWith("kbsub"))
                    저장.사용컴퓨터 = "kbmain";
                else if (임시컴.StartsWith("sub"))
                    저장.사용컴퓨터 = "main";
                else if (임시컴.StartsWith("dzsub"))
                    저장.사용컴퓨터 = "dzmain";
                else
                    저장.사용컴퓨터 = 임시컴;
            }
            else
                저장.사용컴퓨터 = 임시컴;

            로그파일열기();

            환경설정 설정 = new 환경설정();
            기본설정(설정);

            while (true)
            {
                //초기화
                현재상태 상태 = new 현재상태(설정);
                if (DateTime.Now.Day != _currentLogDay)
                {
                    // 날짜가 바뀌었으면 파일을 닫고 새로 엽니다.
                    로그파일열기();
                }

                if (await 단계02파일검색(상태) == "성공")
                {
                    _ = await HwptoPDF(상태);
                }

                await Task.Delay(TimeSpan.FromSeconds(2));
                txt메시지.Text = "";
                lstHwp.Items.Clear();
                // 숫자 => 이미지 표시
                숫자이미지표시(-999, "HWP");
                숫자이미지표시(-999, "PDF");

            }
        }

        // ==========================================================================================================
        // 01 기본설정 
        // ==========================================================================================================
        private void 기본설정(환경설정 설정)
        {
            try
            {
                //모두 3곳을 수정해야 한다.(맞춤약관, H2PControl, F1)
                Dictionary<string, string> 설정값 = new Dictionary<string, string>();
                if (new System.IO.FileInfo(저장.DB설정파일path).Exists)
                {
                    string[] 설정파일 = System.IO.File.ReadAllLines(저장.DB설정파일path, Encoding.GetEncoding(949));
                    foreach (string tmp설정 in 설정파일)
                    {
                        string[] 분리 = tmp설정.Split('\t');
                        if (분리.Length < 2) continue;

                        string key = 분리[0].Trim();
                        string value = 분리[1].Trim();
                        if (key == "접속" || key == "FTP")
                        {
                            if (분리.Length < 3) continue;

                            string 키1 = 분리[1].Trim();
                            string 값1 = 분리[2].Trim();

                            if (key == "접속") {
                                키1.Split('|')
                                   .ToList()
                                   .ForEach(x =>
                                   {
                                       if (!설정값.ContainsKey($"{key}{x}"))
                                           설정값.Add($"{key}{x}", 값1);
                                   });
                            }
                            else if (key == "FTP") {
                                if (!설정값.ContainsKey($"{key}{키1}"))
                                    설정값.Add($"{key}{키1}", 값1);
                            }

                        }
                        else if (key == "병렬") {
                            value.Split('|')
                                 .Where(x => x.Contains("main") || x.Contains("sub"))
                                 .ToList()
                                 .ForEach(x =>
                                 {
                                     if (!설정값.ContainsKey(($"{key}{x}")))
                                         설정값.Add($"{key}{x}", x.Contains("main") ? "main" : "sub");
                                 });
                        }
                        else {
                            if (!설정값.ContainsKey(key))
                                설정값.Add(key.ToUpper(), value);
                        }
                    }
                }

                string 컴유형 = "";
                if (설정값.TryGetValue($"병렬{저장.사용컴퓨터}", out string tmp컴유형))
                    컴유형 = tmp컴유형;

                //모두 3곳을 수정해야 한다.(맞춤약관, H2PControl, F1)
                switch (컴유형)
                {
                    //병렬처리
                    case "sub":
                        //끝에서 4글자
                        설정.상태컴퓨터 = 저장.사용컴퓨터.Substring(저장.사용컴퓨터.Length - 4, 4); // ex) sub1, sub2, sub3,..... 반드시 병렬컴퓨터는 컴퓨터명을 sub포함해야 한다..
                        int 인수 = 설정.상태컴퓨터.IndexOf("sub") + 3;
                        int 숫자 = -1;
                        if (int.TryParse(설정.상태컴퓨터.Substring(인수), out 숫자))
                        {
                            설정.처리구분 = string.Format("{0:00}", 숫자);
                        }
                      
                        break;
                    default:
                        설정.상태컴퓨터 = "main";   // main컴, 일반컴 모두 main컴으로 처리
                        설정.처리구분 = "00";
                        break;
                }

                //data테이블 접속관련
                if (설정값.TryGetValue($"접속{저장.사용컴퓨터}", out string 접속) && 파라메타 != "앱실행_싱글처리")
                    설정.dataTable접속 = 접속;
                else
                    설정.dataTable접속 = 설정값["접속LOCALDB"];

            }
            catch (Exception ee )
            {
                로그내용 = string.Format("{0:yy.MM.dd HH:mm:ss}[기본설정 Er02] {1}==>{2}", DateTime.Now, ee.Message, ee.StackTrace);
                로그기록(로그내용);
            }
        }

        // ==========================================================================================================
        // 02 기본화면 
        // ==========================================================================================================
        private void 기본화면()
        {
            this.SuspendLayout();   // 레이아웃 갱신 멈춤

            this.Text = string.Format("H2P_F{0}", 저장.개별폴더);
            this.MaximizeBox = false;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;

            int index = Convert.ToInt32(저장.개별폴더) - 1;
            int width = 213;
            int height = 400;

            int row = index / 4;
            int col = index % 4;

            // [중요 수정 1] 타겟 모니터를 확실하게 정합니다.
            // 윈도우 설정에서 '주 모니터'로 체크된 곳에 띄우려면 PrimaryScreen을 씁니다.
            Screen targetScreen = System.Windows.Forms.Screen.PrimaryScreen;

            // (만약 주 모니터 상관없이 무조건 '물리적으로 오른쪽'에 있는 모니터에 띄우고 싶다면 아래 주석을 사용하세요)
            // Screen targetScreen = System.Windows.Forms.Screen.AllScreens.OrderByDescending(s => s.Bounds.X).First();

            // [중요 수정 2] 해당 모니터의 시작 좌표(Left)와 너비(Width)를 모두 가져옵니다.
            int screenLeft = targetScreen.WorkingArea.Left; // 모니터가 시작되는 X 좌표 (0이 아닐 수 있음)
            int screenWidth = targetScreen.WorkingArea.Width;

            int totalGroupWidth = 4 * width;

            // [중요 수정 3] X 좌표 계산 시 screenLeft를 더해줘야 해당 모니터 안에서 위치를 잡습니다.
            // 공식: [모니터 시작점] + ([모니터 너비] - [앱 전체 너비]) + [내 순서 너비]
            int X = screenLeft + (screenWidth - totalGroupWidth) + (col * width);

            int startY = 50;
            // Y좌표도 모니터 높이 시작점(Top)을 더해주는 것이 안전합니다 (보통은 0이지만 세로배치 등 대비)
            int Y = targetScreen.WorkingArea.Top + startY + (row * height);

            this.Location = new Point(X, Y);
            this.Size = new Size(width, height);


            //경로 = string.Format("{0}{1}\\", 상태.출력폴더, 저장.개별폴더);

            imgHwp = new PictureBox[5];
            imgPdf = new PictureBox[5];
            int 인수 = -1;
            string 이름 = "";

            // 재미삼아 ^^
            foreach (System.Windows.Forms.Control control in this.Controls)
            {
                이름 = control.Name;
                if (이름.StartsWith("picHwp"))
                {
                    // 이름뒷 2자리는 숫자로 되어 있다
                    TmpControl(이름, out 인수);
                    imgHwp[인수] = control as PictureBox;
                }
                else if (이름.StartsWith("picPdf"))
                {
                    TmpControl(이름, out 인수);
                    imgPdf[인수] = control as PictureBox;
                }
            }
            this.ResumeLayout(false);
        }
        private void TmpControl(string _controlName, out int 인수)
        {
            int 길이 = -1;
            길이 = _controlName.Length;
            인수 = Convert.ToInt16(_controlName.Substring(길이 - 2, 2));  // 이름뒷 2자리는 숫자로 되어 있다
        }

        // ==========================================================================================================
        //  숫자이미지표시 
        // ==========================================================================================================
        private void 숫자이미지표시(int 숫자, string 인자)
        {
            string numberString = (숫자 == -999) ? "0000" : 숫자.ToString();
            List<int> 숫자분리 = new List<int>();

            int 인수 = 0;
            for (int i = numberString.Length - 1; i >= 0; i--)
            {
                숫자분리.Add(int.Parse(numberString.Substring(i, 1)));
                인수++;
                if (인자 == "HWP")
                    imgHwp[인수].Image = Image.FromFile(string.Format("{0}\\img\\{1}.png", Application.StartupPath, 숫자분리[인수 - 1]));
                else if (인자 == "PDF")
                {
                    imgPdf[인수].Image = Image.FromFile(string.Format("{0}\\img\\{1}.png", Application.StartupPath, 숫자분리[인수 - 1]));
                }
            }
            
        }

        // ==========================================================================================================
        //  03 *.단계02파일 검색
        // ==========================================================================================================
        private async Task<string> 단계02파일검색(현재상태 상태)
        {
            int 재시도 = 0;
            string 실행여부 = "성공";
            int 대기중cnt = 0;
            this.Opacity = 0.1;
            while (true)
            {

                try
                {
                    DirectoryInfo 감시폴더 = new DirectoryInfo(string.Format("{0}감시\\", 저장.기본출력폴더));

                    bool 대기여부 = true;

                    foreach (FileInfo 감시파일 in 감시폴더.GetFiles("*.단계02"))
                    {
                        상태.groupKey = System.IO.Path.GetFileNameWithoutExtension(감시파일.Name); //확장자제거
                        string 시작파일 = $"{감시폴더.FullName}\\{상태.groupKey}.F{저장.개별폴더}_시작";
                        상태.종료파일 = $"{감시폴더.FullName}\\{상태.groupKey}.F{저장.개별폴더}_종료";

                        if (new FileInfo(상태.종료파일).Exists) //종료파일이 존재하면 처리가 완료된 상태
                            continue;

                        if (!new FileInfo(시작파일).Exists)
                        {
                            string 시작시간 = $"{DateTime.Now}";
                            File.WriteAllText(시작파일, 시작시간, Encoding.GetEncoding(949));
                        }

                        string 내용 = File.ReadAllText(감시파일.FullName, Encoding.GetEncoding(949)).Split('|')[2];
                        상태.출력폴더 = 내용;
                        상태.F1출력폴더 = string.Format("{0}{1}\\", 상태.출력폴더, 저장.개별폴더);

                        txt메시지.Text = $"작업을 시작 합니다.(I LOVE 천재소년)";
                        대기여부 = false;
                        this.Opacity = 0.7;
                        break;
                    }
                    if (대기여부)
                    {
                        대기중cnt++;
                        txt메시지.Text = $"작업 대기중.....[{대기중cnt: #,000}]";
                        await Task.Delay(TimeSpan.FromSeconds(2));
                        continue;
                    } 

                    break;
                }
                catch (Exception)
                {

                    재시도++;
                    if (재시도 <= 3)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(5));
                        continue;
                    }
                    실행여부 = "실패";
                }
            }

            return 실행여부;
        }

        // ==========================================================================================================
        // 04 HWP2PDF 변환 작업
        //   핫폴더 개념으로 생성되어 있는 한글파일을 PDF파일로 변경한다.
        //   
        // ==========================================================================================================

        [HandleProcessCorruptedStateExceptions]
        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        // UI 컨트롤 안전 업데이트 메서드
        private void 화면메시지출력(string 메시지)
        {
            if (txt메시지.InvokeRequired)
            {
                txt메시지.BeginInvoke(new Action(() => txt메시지.Text = 메시지));
            }
            else
            {
                txt메시지.Text = 메시지;
            }
        }

        private async Task<bool> HwptoPDF(현재상태 상태)
        {
            Type hwpType = Type.GetTypeFromProgID("HWPFrame.HwpObject");

            // HWP COM 객체 초기화 로컬 함수
            async Task<dynamic> 초기화Hwp()
            {
                if (hwp != null)
                {
                    try
                    {
                        var check = hwp.EditMode; // 살아있는지 체크
                        return hwp; // 살아있다면 기존 것 리턴
                    }
                    catch { hwp = null; } // 죽었으면 아래로 진행해서 새로 생성
                }

                dynamic instance = null;

                // COM 객체 생성 3회 재시도
                for (int retry = 0; retry < 3; retry++)
                {
                    try
                    {
                        instance = await Task.Run(() => Activator.CreateInstance(hwpType));
                        break; // 성공하면 즉시 탈출
                    }
                    catch (Exception ex)
                    {
                        if (retry == 2)
                        {
                            throw new Exception($"한컴 COM 객체 생성 실패: {ex.Message}");
                        }
                        await Task.Delay(2000);
                    }
                }

                instance.RegisterModule("FilePathCheckDLL", "FilePathCheckerModuleExample");
                instance.SetMessageBoxMode(0x00000021); // 읽기전용

                currentHwpPID = 0;
                int hwnd = 0;

                // 한컴이 켜질 때까지 기다렸다가 핸들 얻기
                for (int i = 0; i < 30; i++)
                {
                    try
                    {
                        hwnd = instance.XHwpWindows.Item(0).WindowHandle;
                        if (hwnd != 0) break;
                    }
                    catch { }
                    await Task.Delay(100);
                }

                if (hwnd != 0)
                {
                    IntPtr handle = new IntPtr(hwnd);
                    GetWindowThreadProcessId(handle, out uint pid);
                    currentHwpPID = (int)pid;

                    // PID 폴더 생성 및 파일 저장 (F1.PID)
                    try
                    {
                        DirectoryInfo PID폴더 = new DirectoryInfo(string.Format("{0}PID\\", 저장.기본출력폴더));
                        if (!PID폴더.Exists) PID폴더.Create();
                        File.WriteAllText(_pidFilePath, currentHwpPID.ToString());
                    }
                    catch { }
                }

                // 화면 숨기기
                try { instance.XHwpWindows.Item(0).Visible = false; } catch { }

                hwp = instance;
                return instance;
            }

            int 재시도 = 0;
            int 파일대기 = 0;
            HashSet<string> 완료Hwp = new HashSet<string>();

            while (true)
            {
                try
                {
                    string 완료폴더 = Path.Combine(상태.F1출력폴더, "@완료");
                    if (!Directory.Exists(완료폴더)) Directory.CreateDirectory(완료폴더);

                    // 1-1. 완료폴더 파일 목록 가져오기
                    HashSet<string> 완료Files = new HashSet<string>(
                                            new DirectoryInfo(완료폴더).GetFiles("Tmp_*.hwp").Select(f => f.Name),
                                            StringComparer.OrdinalIgnoreCase);

                    // 1-2. 현재폴더 파일 목록 가져오기
                    FileInfo[] 현재Files = new DirectoryInfo(상태.F1출력폴더).GetFiles("Tmp_*.hwp");

                    // 1-3. 현재 폴더의 파일 중 완료폴더에 이미 있는 것(True: 찌꺼기)과 없는 것(False: 찐 대기파일) 분리
                    var 분리된목록 = 현재Files.ToLookup(f => 완료Files.Contains(f.Name));

                    var HWPFiles = 분리된목록[false]
                                    .OrderBy(f => f.LastWriteTime)
                                    .ThenBy(f => f.Name)
                                    .ToList();

                    var 중복Files = 분리된목록[true].ToList();

                    int 실제남은파일cnt = HWPFiles.Count;
                    string 종료확인 = string.Format("{0}F{1}.약관완료", 상태.F1출력폴더, 저장.개별폴더);

                    if (실제남은파일cnt > 0 && hwp == null)
                    {
                        try
                        {
                            hwp = await 초기화Hwp();
                        }
                        catch (Exception exInit)
                        {
                            화면메시지출력($"초기화 실패: {exInit.Message}");
                            로그기록($"{DateTime.Now:HH:mm:ss} [치명적오류] {exInit.Message}");
                            return false;
                        }
                    }

                    // =========================================================================================
                    // ★ [단계 3 수정] 앱 종료 직전 @완료 폴더의 .xxx 패자부활전 재심사 로직
                    // =========================================================================================
                    if (new FileInfo(종료확인).Exists && 실제남은파일cnt == 0)
                    {
                        FileInfo[] 격리된xxx파일들 = new DirectoryInfo(완료폴더).GetFiles("*.xxx");
                        if (격리된xxx파일들.Length > 0)
                        {
                            bool 재시도할파일있음 = false;
                            List<FileInfo> 영구실패파일들 = new List<FileInfo>();

                            foreach (FileInfo xxx파일 in 격리된xxx파일들)
                            {
                                int strike = GetStrikeCount(xxx파일.Name);
                                if (strike < 3)
                                {
                                    재시도할파일있음 = true;
                                }
                                else
                                {
                                    영구실패파일들.Add(xxx파일);
                                }
                            }

                            // 1. 3회 이상 실패한 진짜 불량 문서(.xxx)는 최종 출력폴더(상태.출력폴더)로 내보냄
                            foreach (FileInfo 영구실패 in 영구실패파일들)
                            {
                                try
                                {
                                    string 순수파일명 = Path.GetFileNameWithoutExtension(영구실패.Name);
                                    while (순수파일명.StartsWith("Tmp_", StringComparison.OrdinalIgnoreCase))
                                    {
                                        순수파일명 = 순수파일명.Substring(4);
                                    }
                                    string 최종오류파일 = Path.Combine(상태.출력폴더, 순수파일명 + ".xxx");
                                    File.Copy(영구실패.FullName, 최종오류파일, true);
                                    영구실패.Delete(); // @완료 폴더에서 제거

                                    string 확정로그 = $"[3진아웃 확정] {순수파일명}.xxx 3회 변환 실패로 최종 에러 처리";
                                    화면메시지출력(확정로그);
                                    로그기록($"{DateTime.Now:yy.MM.dd HH:mm:ss} {확정로그}");
                                }
                                catch { }
                            }

                            // 2. 1~2회 실패하여 기회가 남은 파일이 있다면 상위 폴더로 복구 후 루프 재진입!
                            if (재시도할파일있음)
                            {
                                HwpProcessKill(false); // 한글 엔진 정리
                                await Task.Delay(2000); // OS 핸들 해제 대기

                                // Temp 및 락 파일 청소
                                완전한Temp삭제();

                                foreach (FileInfo xxx파일 in new DirectoryInfo(완료폴더).GetFiles("*.xxx"))
                                {
                                    try
                                    {
                                        // @완료\Tmp_계약서.xxx -> 상위폴더\Tmp_Tmp_계약서.hwp 로 이동
                                        string 부활파일명 = "Tmp_" + Path.GetFileNameWithoutExtension(xxx파일.Name) + ".hwp";
                                        string 상위폴더대상 = Path.Combine(상태.F1출력폴더, 부활파일명);

                                        File.Copy(xxx파일.FullName, 상위폴더대상, true);
                                        xxx파일.Delete(); // @완료 폴더에서 이동 완료

                                        string 부활로그 = $"[패자부활 이동] {xxx파일.Name} -> {부활파일명} (상위 폴더 복구)";
                                        화면메시지출력(부활로그);
                                        로그기록($"{DateTime.Now:yy.MM.dd HH:mm:ss} {부활로그}");
                                    }
                                    catch (Exception exMove)
                                    {
                                        로그기록($"{DateTime.Now:yy.MM.dd HH:mm:ss} [패자부활 복구실패] {exMove.Message}");
                                    }
                                }

                                // 깨끗한 새 한글 엔진 가동 후 while(true) 다음 루프로 진행하여 변환 시작!
                                hwp = await 초기화Hwp();
                                continue; // ★ 핵심: 상위 폴더에 파일이 생겼으므로 실제남은파일cnt > 0 이 되어 자연스럽게 변환됨!
                            }
                        }

                        // ★ 더 이상 재시도할 .xxx 가 없을 때 비로소 진짜 종료!
                        string 종료시간 = $"{DateTime.Now}";
                        File.WriteAllText(상태.종료파일, 종료시간, Encoding.GetEncoding(949));
                        화면메시지출력("작업을 종료 합니다.");
                        lstHwp.Items.Clear();

                        if (hwp != null)
                        {
                            HwpProcessKill(false);
                        }
                        return true;
                    }
                    // 2-2. 마지막 1개 남은 데이터 처리
                    else if (new FileInfo(종료확인).Exists && 실제남은파일cnt == 1)
                    {
                        try { hwp.Clear(1); } catch { HwpProcessKill(false); await Task.Delay(500); hwp = await 초기화Hwp(); }

                        foreach (string 파일경로 in 완료Hwp)
                        {
                            try
                            {
                                if (File.Exists(파일경로))
                                {
                                    string 완료경로 = Path.Combine(완료폴더, Path.GetFileName(파일경로));
                                    if (!File.Exists(완료경로))
                                        File.Copy(파일경로, 완료경로, true);

                                    File.Delete(파일경로);
                                }
                            }
                            catch (Exception)
                            {
                                string 에러문구 = $"[삭제지연] {Path.GetFileName(파일경로)}";
                                화면메시지출력(에러문구);
                            }
                        }
                        완료Hwp = new HashSet<string>(); // 초기화
                    }
                    // 2-3. 앱 대기 상태 (변환할 파일이 없을 때)
                    else if (실제남은파일cnt == 0)
                    {
                        파일대기++;
                        if (파일대기 >= 60) // 최대 5분까지 대기
                        {
                            로그내용 = "약관파일(Hwp) 생성오류";
                            로그기록($"{DateTime.Now: yy.MM.dd HH:mm:ss} [HwptoPDF Er02] {로그내용}");
                            화면메시지출력(로그내용);
                            return false;
                        }
                        await Task.Delay(TimeSpan.FromSeconds(5));
                        재시도 = 0;
                        continue;
                    }

                    파일대기 = 0;
                    재시도 = 0;

                    if (중복Files.Count > 0)
                    {
                        await Task.Run(() => {
                            foreach (var f in 중복Files)
                            {
                                try { if (f.Exists) f.Delete(); } catch { }
                            }
                        });
                    }

                    foreach (var 파일 in HWPFiles)
                    {
                        int 중복인수 = lstHwp.Items.IndexOf(파일.Name);
                        if (중복인수 == -1)
                            lstHwp.Items.Add(파일.Name);
                    }

                    string PDF파일 = "";
                    int 인수 = -1;

                    try
                    {
                        숫자이미지표시(lstHwp.Items.Count, "HWP");

                        for (int i = 0; i < lstHwp.Items.Count; i++)
                        {
                            인수 = i;
                            if (lstHwp.GetItemChecked(i))
                                continue;

                            string 한글파일 = string.Format("{0}{1}", 상태.F1출력폴더, lstHwp.Items[i]);
                            string 저장파일명 = System.IO.Path.GetFileNameWithoutExtension(한글파일).Replace("Tmp_", "");
                            PDF파일 = string.Format("{0}{1}.pdf", 상태.출력폴더, 저장파일명);

                            string 현재파일명 = Path.GetFileName(한글파일);

                            bool fileConverted = false;
                            int maxAttempts = 2;

                            for (int attempt = 0; attempt < maxAttempts; attempt++)
                            {
                                try
                                {
                                    FileInfo fi = new FileInfo(한글파일);
                                    double fileSizeKB = fi.Length / 1024.0;
                                    int chunks = (int)Math.Ceiling(fileSizeKB / 500.0);
                                    if (chunks == 0) chunks = 1;

                                    int timeoutSeconds = chunks * 40 + 300;
                                    if (attempt == 1) timeoutSeconds += 30;

                                    if (hwp.Open(한글파일, "HWP", "forceopen:true"))
                                    {
                                        foreach (string 파일경로 in 완료Hwp)
                                        {
                                            try
                                            {
                                                if (File.Exists(파일경로))
                                                {
                                                    string 완료파일 = Path.Combine(상태.F1출력폴더, "@완료", Path.GetFileName(파일경로));
                                                    if (!File.Exists(완료파일)) File.Copy(파일경로, 완료파일, true);
                                                    File.Delete(파일경로);
                                                }
                                            }
                                            catch (Exception)
                                            {
                                                화면메시지출력($"[삭제지연] {Path.GetFileName(파일경로)}");
                                            }
                                        }
                                        완료Hwp = new HashSet<string>();

                                        await Task.Delay(100);
                                        hwp.EditMode = 0;
                                        var act = hwp.CreateAction("FileSaveAs_S");
                                        var set = act.CreateSet();
                                        act.GetDefault(set);
                                        set.SetItem("FileName", PDF파일);
                                        set.SetItem("Format", "PDF");

                                        bool isSuccess = false;
                                        bool isTimeout = false;

                                        using (CancellationTokenSource watchdogCts = new CancellationTokenSource())
                                        {
                                            Task watchdogTask = Task.Delay(TimeSpan.FromSeconds(timeoutSeconds), watchdogCts.Token)
                                                .ContinueWith(t =>
                                                {
                                                    if (!t.IsCanceled)
                                                    {
                                                        isTimeout = true;
                                                        HwpProcessKill(true);
                                                    }
                                                });

                                            try
                                            {
                                                isSuccess = (bool)act.Execute(set);
                                            }
                                            catch (Exception)
                                            {
                                                if (isTimeout) throw new TimeoutException($"[PDF 변환지연] 제한시간 {timeoutSeconds}초 초과");
                                                throw;
                                            }
                                            finally
                                            {
                                                watchdogCts.Cancel();
                                                try { if (set != null) { Marshal.FinalReleaseComObject(set); set = null; } } catch { }
                                                try { if (act != null) { Marshal.FinalReleaseComObject(act); act = null; } } catch { }
                                            }
                                        }

                                        if (isSuccess)
                                        {
                                            await Task.Delay(100);
                                            lstHwp.SetItemChecked(i, true);
                                            lstHwp.SelectedIndex = i;
                                            숫자이미지표시(i + 1, "PDF");

                                            완료Hwp.Add(한글파일);
                                            fileConverted = true;
                                            break;
                                        }
                                        else
                                        {
                                            throw new Exception("Save 실패");
                                        }
                                    }
                                    else
                                    {
                                        throw new Exception("Open 실패");
                                    }
                                }
                                catch (Exception ex)
                                {
                                    if (치명적오류(ex))
                                    {
                                        string 에러문구1 = $"[치명적오류] 통신 끊김. 파일 재시도 없이 앱을 즉시 재시작합니다.";
                                        화면메시지출력(에러문구1);
                                        로그기록($"{DateTime.Now:HH:mm:ss} {에러문구1}");
                                        throw;
                                    }

                                    bool isTimeoutError = ex is TimeoutException || ex.Message.Contains("변환지연");

                                    if (!isTimeoutError)
                                    {
                                        string 에러문구Fast = $"[변환실패] 일시적 지연이 아닌 오류. 재시도 생략 후 즉시 격리합니다: {ex.Message}";
                                        화면메시지출력(에러문구Fast);
                                        로그기록($"{DateTime.Now:HH:mm:ss} {에러문구Fast}");
                                        break;
                                    }

                                    string 에러문구2 = $"[오류/지연] {lstHwp.Items[i]} - 변환 실패 (시도 {attempt + 1}/{maxAttempts}): {ex.Message}";
                                    화면메시지출력(에러문구2);
                                    로그기록($"{DateTime.Now:HH:mm:ss} {에러문구2}");

                                    try
                                    {
                                        HwpProcessKill(false);
                                        await Task.Delay(1500);
                                        hwp = await 초기화Hwp();
                                    }
                                    catch (Exception exInit)
                                    {
                                        string 에러문구1 = $"[초기화실패] COM 객체 재생성 실패. 앱을 재시작합니다: {exInit.Message}";
                                        화면메시지출력(에러문구1);
                                        로그기록($"{DateTime.Now:HH:mm:ss} {에러문구1}");
                                        throw;
                                    }
                                }
                            } // End For (재시도 루프 끝)

                            // =========================================================================================
                            // ★ [단계 2 수정] 변환 실패 시: 상태.출력폴더가 아닌 본인 @완료 폴더에 .xxx 로 보관!
                            // =========================================================================================
                            if (!fileConverted)
                            {
                                HwpProcessKill(false); // 한컴 객체 확실히 끄기
                                await Task.Delay(500);

                                string 완료폴더1 = Path.Combine(상태.F1출력폴더, "@완료");
                                if (!Directory.Exists(완료폴더1)) Directory.CreateDirectory(완료폴더1);

                                int strike = GetStrikeCount(현재파일명) + 1;
                                string 에러문구 = $"[변환포기] HWP 오류 감지 (Strike {strike}/3). 패자부활전 대기를 위해 @완료 폴더에 임시 보관: {현재파일명}";
                                로그기록($"{DateTime.Now:HH:mm:ss} {에러문구}");
                                화면메시지출력(에러문구);

                                try
                                {
                                    // ★ 상태.출력폴더가 아닌, @완료 폴더 내에 .xxx 확장자로 보관하여 대기열 등록!
                                    string 격리xxx파일 = Path.Combine(완료폴더1, Path.GetFileNameWithoutExtension(현재파일명) + ".xxx");

                                    if (File.Exists(한글파일))
                                    {
                                        File.Copy(한글파일, 격리xxx파일, true);
                                    }
                                }
                                catch (Exception exCopy)
                                {
                                    로그기록($"{DateTime.Now:HH:mm:ss} [파일격리오류] 복사 실패: {exCopy.Message}");
                                }

                                // 역할을 다한 Tmp_Tmp_ 마커 파일 등 찌꺼기가 있다면 조용히 삭제 시도
                                try { File.Delete(Path.Combine(완료폴더1, "Tmp_" + 현재파일명)); } catch { }

                                // 루프가 멈추지 않도록 체크박스 선택 처리 및 원본 삭제 예약
                                lstHwp.SetItemChecked(i, true);
                                완료Hwp.Add(한글파일);

                                try
                                {
                                    hwp = await 초기화Hwp();
                                }
                                catch (Exception)
                                {
                                    throw new Exception("격리 처리 후 HWP 초기화 실패");
                                }
                            }
                        } // End For
                    }
                    catch (Exception e1)
                    {
                        if (치명적오류(e1))
                        {
                            throw;
                        }

                        HwpProcessKill(false);
                        화면메시지출력(e1.Message);
                        로그기록($"{DateTime.Now:HH:mm:ss} {e1.Message}");
                        Application.DoEvents();
                        await Task.Delay(1000);
                        hwp = await 초기화Hwp();
                    }

                    await Task.Delay(100);
                }
                catch (Exception e2)
                {
                    재시도++;
                    if (재시도 <= 3)
                    {
                        if (치명적오류(e2))
                        {
                            string 에러문구 = "[치명적오류] RPC 서버 연결 끊김. 앱을 즉시 종료합니다.";
                            로그기록($"{DateTime.Now:HH:mm:ss} {에러문구}");

                            try
                            {
                                HwpProcessKill(true);
                                Process.GetCurrentProcess().Kill();
                            }
                            catch
                            {
                                Environment.Exit(0);
                            }
                            return false;
                        }

                        string 에러상세 = string.Format("{0}==>{1}", e2.Message, e2.StackTrace);
                        화면메시지출력(에러상세);
                        로그기록($"{DateTime.Now:HH:mm:ss} {에러상세}");
                        await Task.Delay(TimeSpan.FromSeconds(2));
                        continue;
                    }
                    try
                    {
                        HwpProcessKill(true);
                        Process.GetCurrentProcess().Kill();
                    }
                    catch
                    {
                        Environment.Exit(0);
                    }
                }
            }
        }

        // =========================================================================================
        // ★ [헬퍼 1] 파일명 앞의 "Tmp_" 개수를 세어 실패 횟수(Strike)를 계산 (3진 아웃 판별용)
        // =========================================================================================
        private int GetStrikeCount(string fileName)
        {
            int count = 0;
            int index = 0;
            while ((index = fileName.IndexOf("Tmp_", index, StringComparison.OrdinalIgnoreCase)) != -1)
            {
                count++;
                index += 4; // "Tmp_".Length
            }
            return count;
        }

        private void 완전한Temp삭제()
        {

            //파일삭제 : del /q /f /s "%TEMP%\*" 2>nul 
            //폴더삭제 : for /D %p in ("%TEMP%\*") do rd /s /q "%p" 2>nul
            try
            {
                로그기록($"{DateTime.Now:yy.MM.dd HH:mm:ss} [시스템-알림] 정기 재시작 전 Temp 폴더 무차별 청소를 시작합니다.");

                System.Diagnostics.ProcessStartInfo pri = new System.Diagnostics.ProcessStartInfo();
                pri.FileName = "cmd.exe";
                // 찌꺼기 파일과 빈 폴더까지 모두 싹 지우는 명령어 (에러 메시지 숨김 처리 포함)
                pri.Arguments = "/c del /q /f /s \"%TEMP%\\*\" 2>nul & for /D %p in (\"%TEMP%\\*\") do rd /s /q \"%p\" 2>nul";
                pri.CreateNoWindow = true;          // 검은 CMD 창 숨김
                pri.UseShellExecute = false;

                using (System.Diagnostics.Process p = System.Diagnostics.Process.Start(pri))
                {
                    로그기록($"{DateTime.Now:yy.MM.dd HH:mm:ss} [시스템-알림] Temp 폴더 청소중.....");
                    // 삭제가 완료될 때까지 최대 15초간 대기 (동기화)
                    p.WaitForExit(15000);
                }

                로그기록($"{DateTime.Now:yy.MM.dd HH:mm:ss} [시스템-알림] Temp 폴더 청소 완료.");
            }
            catch (Exception ex)
            {
                로그기록($"{DateTime.Now:yy.MM.dd HH:mm:ss} [오류] Temp 폴더 청소 중 예외 발생: {ex.Message}");
            }
        }





        private bool 치명적오류(Exception ex)
        {
            if (ex == null || string.IsNullOrEmpty(ex.Message))
                return false;

            string msg = ex.Message;

            // 추가적인 치명적 COM 오류 코드가 발견되면 여기에만 추가하면 전체 앱에 적용됩니다.
            return msg.Contains("0x800706BA") ||
                   msg.Contains("RPC 서버") ||
                   msg.Contains("0x800706BE") ||
                   msg.Contains("원격 프로시저를 호출하지");
        }


        private void KillMyZombieHwp() 
        {
            try
            {
                // 1. PID 파일이 존재하는지 확인 (_pidFilePath는 "Output\PID\F1.PID")
                if (string.IsNullOrEmpty(_pidFilePath) || !File.Exists(_pidFilePath)) return;

                // 2. 파일에서 PID 읽기
                string pidStr = File.ReadAllText(_pidFilePath);
                if (int.TryParse(pidStr, out int targetPid))
                {
                    // 3. 해당 PID 프로세스 찾아서 사살
                    try
                    {
                        Process p = Process.GetProcessById(targetPid);
                        if (!p.HasExited)
                        {
                            p.Kill();
                            p.WaitForExit(100);
                        }
                    }
                    catch (ArgumentException) { /* 이미 없음 */ }
                    catch { }
                }

                // 4. 파일 삭제 (청소)
                File.Delete(_pidFilePath);
            }
            catch { }
        }

        private void HwpProcessKill(bool 강제종료 = false)
        {
            try
            {
                Process p = null;
                if (currentHwpPID > 0)
                {
                    try { p = Process.GetProcessById(currentHwpPID); } catch { }
                }

                // 1. 강제종료가 아닐 경우: 우아한 종료(Graceful Shutdown) 시도
                if (!강제종료)
                {
                    if (hwp != null)
                    {
                        // ★ COM 통신 멈춤(Freeze) 방지를 위한 비동기 타임아웃 적용
                        var cleanupTask = Task.Run(() =>
                        {
                            try { hwp.Clear(1); } catch { }
                            try { hwp.Quit(); } catch { }
                            try { Marshal.FinalReleaseComObject(hwp); } catch { }
                        });

                        // ★ 최대 1.5초까지만 기다림. 
                        // 통신이 끊겨서 hwp.Quit()이 멈춰도 1.5초 뒤에 미련 없이 다음 줄로 넘어갑니다.
                        cleanupTask.Wait(1500);
                    }

                    hwp = null;
                    GC.Collect();
                    //GC.WaitForPendingFinalizers();

                    if (p != null)
                    {
                        try { p.WaitForExit(1000); } catch { }
                    }
                }

                // 2. 프로세스 확인 사살 
                // 위에서 Wait()가 타임아웃 났거나, 우아한 종료를 했는데도 안 죽은 좀비들만 여기서 처형됩니다.
                if (p != null)
                {
                    try
                    {
                        if (!p.HasExited)
                        {
                            p.Kill();
                            p.WaitForExit(1000);
                        }
                    }
                    catch { }
                }
            }
            catch { }
            finally
            {
                // 다음 실행을 위해 PID와 객체를 확실하게 초기화
                currentHwpPID = 0;
                hwp = null;
            }
        }

        //========================================================================================================================================
        // 공통
        //========================================================================================================================================


        // =============================================================================
        //  로그파일생성(공통)
        // =============================================================================
        private void 로그파일열기()
        {
            string 로그폴더생성 = 폴더생성(저장.로그경로, $"{DateTime.Now:yyyy.MM}");
            로그폴더생성 = 폴더생성(로그폴더생성, $"{DateTime.Now:dd}");

            DateTime now = DateTime.Now; // 현재 시간

            // 1. 기존에 열린 파일이 있다면 닫아줍니다 (메모리 해제 및 저장)
            if (Logwr != null) { Logwr.Flush(); Logwr.Close(); Logwr = null; }
            if (Logfs != null) { Logfs.Close(); Logfs = null; }

            string 로그파일 = string.Format("{0}({1:yyMMdd})F{2}.log", 로그폴더생성, DateTime.Now, 저장.개별폴더);
            try
            {
                Logfs = new FileStream(로그파일, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
                Logwr = new StreamWriter(Logfs, Encoding.GetEncoding(949));

                // 2. 파일이 성공적으로 열렸을 때의 날짜(Day)를 저장합니다.
                _currentLogDay = now.Day;
            }
            catch (Exception) { }
        }

        // =============================================================================
        //  로그출력(공통)
        // =============================================================================
        private void 로그기록(string 내용)
        {
            try
            {
                Logwr.WriteLine(내용); Logwr.Flush();
            }
            catch (Exception) { }
        }

        private string 폴더생성(string 현재폴더, string 하위폴더)
        {
            string 하위폴더확인 = string.Format("{0}{1}\\", 현재폴더, 하위폴더);
            if (new System.IO.DirectoryInfo(하위폴더확인).Exists == false)
                new System.IO.DirectoryInfo(하위폴더확인).Create();
            return 하위폴더확인;
        }

        // =============================================================================
        //  데이터 UPDATE/DELETE 상태(공통)
        // =============================================================================
        private async Task<int> DBUpdate(string SqlStr, string dataTable접속)
        {
            int 완료갯수 = -1;
            try
            {
                using (SqlConnection AdoNet = new SqlConnection(dataTable접속))
                {
                    await AdoNet.OpenAsync();
                    using (SqlCommand sqlcmd = new SqlCommand(SqlStr, AdoNet))
                    {
                        sqlcmd.CommandTimeout = 0;
                        완료갯수 = await sqlcmd.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (Exception)
            {
            }

            return 완료갯수;
        }

        private void frmh2p_FormClosed(object sender, FormClosedEventArgs e)
        {

            try
            {
                KillMyZombieHwp();
            }
            catch { }

            try
            {
                // 로그 파일 닫기
                if (Logwr != null) { Logwr.Close(); Logwr = null; }
                if (Logfs != null) { Logfs.Close(); Logfs = null; }

                // 절전 모드 방지 해제
                PreventSleep.AllowSleep();
            }
            catch { }
            finally
            {
                try {
                    System.Diagnostics.Process.GetCurrentProcess().Kill();
                }
                catch {
                    Environment.Exit(0);
                }
            }
        }
    }

    public class 저장
    {
        public static string 약관DB;
        public static string DB설정파일path;
        public static string 기본입력폴더;
        public static string 기본출력폴더;
        public static string 사용컴퓨터;
        public static string 로그경로;
        public static string 개별폴더;
    }
    public class 현재상태
    {
        public string 상태컴퓨터 { get; set; } = string.Empty;
        public string dataTable접속 { get; set; } = string.Empty;
        public string 처리구분 { get; set; } = string.Empty;
        public string groupKey { get; set; } = string.Empty;
        public string 출력폴더 { get; set; } = string.Empty;
        public  string F1출력폴더 { get; set; } = string.Empty;
        public string 종료파일 { get; set; } = string.Empty;
        public 현재상태() { }

        public 현재상태(환경설정 설정)
        {
            this.상태컴퓨터 = 설정.상태컴퓨터;
            this.처리구분 = 설정.처리구분;
            this.dataTable접속 = 설정.dataTable접속;
        }
    }

    public class 환경설정
    {
        public string 상태컴퓨터 { get; set; } = string.Empty;
        public string 처리구분 { get; set; } = string.Empty;
        public string dataTable접속 { get; set; } = string.Empty;
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

