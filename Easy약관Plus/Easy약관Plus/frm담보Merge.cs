/*
 * Easy약관Plus & Easy약관PDF
 * Copyright (C) 2026 (주)데이타존
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Affero General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * This software uses the iText 7 library, which is licensed under the AGPL-3.0.
 */
using AxHWPCONTROLLib;
using DevExpress.Utils.Menu;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid.Views.Grid;
using HWPCONTROLLib;
using iText.Forms;
using iText.IO.Font;
using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Navigation;
using iText.Kernel.Utils;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Convert = System.Convert;
using Font = System.Drawing.Font;
using Image = System.Drawing.Image;

namespace Easy약관Plus
{
    public partial class frm담보Merge : DevExpress.XtraEditors.XtraForm
    {
        public string[] 담보Merge메인화면event = new string[10];
        public static event 담보Merge메인화면_Handler 담보Merge메인화면_SetMessage;
        public delegate void 담보Merge메인화면_Handler(string[] 메인화면event);
        private StreamWriter Logwr;
        private FileStream Sqlfs; //쿼리파일
        private StreamWriter Sqlwr;
        private AxHwpCtrl[] Ax02;
        private string 로그내용;
        private List<레코드> 레코드s = new List<레코드>();
        private ConcurrentBag<우편파일> 우편s = new ConcurrentBag<우편파일>();
        private Process H2PControl제어;
        private System.Diagnostics.StackTrace 에러;
        private string 메소드명 = "";
        private 현재상태 상태 = new 현재상태();
        private 서식집계 집계결과 = new 서식집계();
        private Dictionary<string, string> 설정값 = new Dictionary<string, string>();
        private ConcurrentDictionary<string, Dictionary<string, List<int>>> KB개정일 = new ConcurrentDictionary<string, Dictionary<string, List<int>>>();
        private ConcurrentDictionary<string, Dictionary<string, List<int>>> KB개정일이전 = new ConcurrentDictionary<string, Dictionary<string, List<int>>>();
        private ConcurrentDictionary<string, List<약관정보>> 전체약관캐시 = new ConcurrentDictionary<string, List<약관정보>>();
        //키1:담보KB, 담보KB_24, 담보KB_23   키2:상품코드에 따른  개정일

        private Label[] lab순서;
        private CancellationTokenSource _약관KBCts;
        private readonly object _약관KBLock = new object();

        private CancellationTokenSource _담보KBCts;
        private readonly object _담보KBLock = new object();


        private CancellationTokenSource _cancellationTokenSource;
        private int _currentLogDay = -1;
        private string[] _lastErrorMsg =  new string [2]; // 에러 발생 시 메시지 저장용(0:크리티컬오류, 1:레코드단위오류)
        private string stepInfo = "";
        private string 파라메타 = "";

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool QueryFullProcessImageName(IntPtr hProcess, int dwFlags, StringBuilder lpExeName, ref int lpdwSize);

        [DllImport("kernel32.dll")]
        private static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        private const int SW_RESTORE = 9;


        private const int PROCESS_QUERY_LIMITED_INFORMATION = 0x1000;
        private System.Windows.Forms.Timer h2pMonitorTimer; // H2P 감시 타이머
        private bool isH2PRestarting = false;               // 중복 재시작 방지 플래그

        public frm담보Merge(string _파라메타)
        {
            if (_파라메타 == "")
            {
                // Easy약관Plus, H2P 두 앱을 모두 강제 종료 했을때 매번 지우는것이 번거로움
                // Table 검색 후 있으면 다시 시작함
                파라메타 = "처음시작";  
            }
            else{
                파라메타 = _파라메타;  //"재시작"
            }

            InitializeComponent();
            
            frmEasy약관.담보Merge_SetMessage += new frmEasy약관.담보Merge_Handler(event_담보Merge_setMessage);
            

            string 컴이름 = SystemInformation.ComputerName.ToLower();
            chk싱글처리.Checked = NetworkManager.고객사선택("", 컴이름) == "KB2" ? true : false;

            //////테스트 
            //////chk싱글처리.Checked = true;

            if (!로그파일열기(DateTime.Now))
                return;

            _ = 화면초기화();
        }
        private void frm담보Merge_Load(object sender, EventArgs e)
        {
        }

        private void frm담보Merge_FormClosing(object sender, FormClosingEventArgs e)
        {
        }
        
        protected override async void OnShown(EventArgs e)
        {
            base.OnShown(e);
            await Task.Delay(500);

            삭제Status();


            string 임시컴 = SystemInformation.ComputerName.ToLower();
            if (임시컴.Contains("sub") || 파라메타 == "재시작")
            {
                담보Merge메인화면event = new string[10];
                담보Merge메인화면event[0] = "맞춤약관시작";
                담보Merge메인화면_SetMessage.Invoke(담보Merge메인화면event);
            }

        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // 1. 진행 중인 비동기 루프 및 작업 강제 취소
            if (_cancellationTokenSource != null)
            {
                _cancellationTokenSource.Cancel(); // 루프 멈춰!
            }

            // 2. 이벤트 구독 해제 및 엑티브엑스(HWP) 리소스 정리
            frmEasy약관.담보Merge_SetMessage -= new frmEasy약관.담보Merge_Handler(event_담보Merge_setMessage);

            if (Ax02 != null)
            {
                foreach (var ctrl in Ax02)
                {
                    if (ctrl != null && !ctrl.IsDisposed) ctrl.Dispose();
                }
            }

            // 3. 싱글처리(독립 워커) 모드일 때만 로컬 찌꺼기 완벽 초기화
            if (chk싱글처리.Checked)
            {
                try
                {
                    // 상태 객체가 비어있을 경우를 대비해 안전한 로컬DB 문자열 강제 세팅
                    string dbConnString = (상태 != null && !string.IsNullOrEmpty(상태.dataTable접속))
                                          ? 상태.dataTable접속
                                          : "Server=localhost; Database=약관; Integrated Security=True;";

                    string[] tables = { "data", "data처리", "Err_data", "Err_data처리" };

                    // DB 데이터 삭제
                    using (System.Data.SqlClient.SqlConnection conn = new System.Data.SqlClient.SqlConnection(dbConnString))
                    {
                        conn.Open();
                        foreach (string table in tables)
                        {
                            using (System.Data.SqlClient.SqlCommand cmd = new System.Data.SqlClient.SqlCommand($"DELETE FROM {table}", conn))
                            {
                                cmd.ExecuteNonQuery();
                            }
                        }
                    }

                    // 감시 및 List 폴더 비우기
                    string baseOutPath = @"d:\Easy약관Plus\";
                    string[] targetDirs = { baseOutPath + @"Output\감시\", baseOutPath + @"List\" };

                    foreach (string dir in targetDirs)
                    {
                        if (System.IO.Directory.Exists(dir))
                        {
                            foreach (string file in System.IO.Directory.GetFiles(dir))
                            {
                                try { System.IO.File.Delete(file); } catch { } // 잠겨있는 파일 예외 무시
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    // 종료 과정에서 발생하는 에러는 프로그램 종료에 지장을 주지 않도록 무시
                }
            }

            // 4. 윈폼이 정상적으로 닫히도록 부모 클래스 호출
            base.OnFormClosing(e);
        }

        public void event_담보Merge_setMessage(string[] 전달인수)
        {
            switch (전달인수[0])
            {
                case "작업시작":
                    string tmp입력폴더 = 전달인수[1];
                    string tmp기본출력폴더 = 전달인수[2];
                    일하는순서(tmp입력폴더, tmp기본출력폴더, 전달인수[0]);
                    break;
            }
        }

        private async Task 화면초기화()
        {
            if (await 실행(() => grdListFile_View(0), "grdListFile_View") == "실패")
            {
                MessageBox.Show("🚫진행불가 : grdListFile_View 초기값 설정에 실패 했습니다.", "grdListFile_View", MessageBoxButtons.OK);
                return;
            }
            if (await 실행(() => grd오류작업(0), "grd오류작업") == "실패")
            {
                MessageBox.Show("🚫진행불가 : grd오류작업 초기값 설정에 실패 했습니다.", "grd오류작업", MessageBoxButtons.OK);
                return;
            }
            if (await 실행(() => 기본화면설정(), "기본화면설정") == "실패")
            {
                MessageBox.Show("🚫진행불가 : 기본화면 초기값 설정에 실패 했습니다.", "기본화면설정", MessageBoxButtons.OK);
                return;
            }

            logdate.EditValue = DateTime.Now;
            lst로그.DrawItem += lst로그_DrawItem;
            lst로그.HighlightedItemStyle = HighlightStyle.Standard; //스킨해제

            _statusChecks = new DevExpress.XtraEditors.CheckEdit[] { chk대기, chk실행, chk오류 };
        }


        /*
           ============  전체 재시작 흐름도 (새벽 6시 정기 재시작 기준) ============

        1단계: Easy약관Plus의 신호 발생 (Self-Signal)
               새벽 6시가 되면 Easy약관Plus 앱이 현재 작업 상태를 확인합니다.
               작업 중이 아니라면(WAITING), Easy약관Plus는 자신의 상태 파일(d:\Easy약관Plus\Output\STATUS\앱감시상태.txt)에 리스타트앱이라고 적어 넣고 무한 대기 상태(동면)에 들어갑니다. (스스로 죽지 않습니다. 죽여주기를 기다립니다.)

        2단계: H2PControl의 감지 및 처형 (Kill & Restart)
               백그라운드에서 돌고 있던 H2PControl 앱이 MonitorTimer_Tick (10초 주기)을 통해 앱감시상태.txt를 읽습니다.
               상태가 리스타트앱인 것을 발견하면, H2PControl 앱은 재시작 프로세스(isRestarting = true)에 돌입합니다.
               H2PControl은 먼저 자신에게 종속된 자식 프로세스들인 f1 ~ f8 앱을 모두 강제 종료(Kill)합니다. (F1프로그램종료() 메서드 호출)
               그 다음, H2PControl은 TCP/IP 통신을 닫아버립니다. (서버/클라이언트 연결 해제)
               그리고 나서 H2PControl은 Easy약관Plus실행() 메서드를 호출하여, 현재 멈춰있는 Easy약관Plus 프로세스를 강제 종료(Kill)하고, 새로운 Easy약관Plus 프로세스를 Start() 합니다.

        3단계: 부활한 Easy약관Plus의 역공 (Kill & Restart)
               새롭게 부활한 Easy약관Plus 앱이 켜지면서 초기화 과정(일하는순서)을 밟습니다.
               이 과정에서 컨트롤러앱실행("앱실행_재시작") 메서드를 호출합니다.
               Easy약관Plus는 현재 떠있는 기존 H2PControl 프로세스를 찾아내서 강제로 죽여버립니다(Kill).
               그리고 5초 정도 대기한 뒤, 새로운 H2PControl 앱을 Start() 시킵니다.

        4단계: 시스템 완전 정상화
               새롭게 켜진 H2PControl은 다시 초기화를 진행하며 소켓 통신을 열고 감시 타이머를 시작합니다.
               Easy약관Plus도 정상적인 핫폴더 감시 및 약관 생성 루프에 돌입합니다.
               데이터가 들어와 약관 생성이 필요해지면, H2PControl이 다시 f1 ~ f8 앱을 띄워 작업을 수행합니다.
        */


        private DateTime _lastRestartDate = DateTime.Now.Date;
        private bool _isWaitingForRestart = false;
        private DevExpress.XtraEditors.CheckEdit[] _statusChecks;

        private async Task StartHeartBeatLoop(CancellationToken token)
        {
            string path = @"d:\Easy약관Plus\Output\STATUS\앱감시상태.txt";

            // 앱 부활 시 혹시 남아있을 무한 대기 상태 강제 해제
            _isWaitingForRestart = false;

            while (!token.IsCancellationRequested) // 앱 종료 신호가 오기 전까지 계속 반복
            {
                // ==========================================================
                // 1. 상태 결정 로직
                // ==========================================================
                string currentStatus = "WAITING";
                string localStepInfo = "";

                if (_lastErrorMsg != null && !string.IsNullOrEmpty(_lastErrorMsg[0]))
                {
                    currentStatus = "ERROR";
                    localStepInfo = _lastErrorMsg[0];
                }
                else if (_lastErrorMsg != null && !string.IsNullOrEmpty(_lastErrorMsg[1]))
                {
                    currentStatus = "RECORDERR";
                    localStepInfo = _lastErrorMsg[1];
                }
                else
                {
                    if (상태 != null && 상태.처리Step != "00")
                        currentStatus = "RUNNING";
                    else
                    {
                        currentStatus = "WAITING";
                        localStepInfo = "처리STEP : 00";
                    }
                }

                // ==========================================================
                // 2. 새벽 6시 기준 30분 동안 대기상태(WAITING) 일 때 재시작
                // ==========================================================
                DateTime now = DateTime.Now;

                if (_isWaitingForRestart)
                {
                    // 이미 재시작 요청을 했으면, H2P가 나를 죽일 때까지 계속 '리스타트앱'만 외침
                    currentStatus = "리스타트앱";
                    localStepInfo = "정기재시작 대기 중...";
                }
                else if (저장.정기재시작 >= 0)
                {
                    int 현재시간 = Convert.ToInt32($"{now:HHmm}");

                    // 회원님의 직관적인 2단계 논리 적용!

                    // 조건 1) 오늘 아직 재시작을 안 했고(날짜가 다르고) && 현재 아무 작업도 안 하는 대기(WAITING) 상태인가?
                    if (_lastRestartDate.Date != now.Date && currentStatus == "WAITING")
                    {
                        // 조건 2) 지금 시간이 설정된 정기재시작 시간(예: 600 ~ 630) 사이인가?
                        if (현재시간 >= 저장.정기재시작 && 현재시간 <= 저장.정기재시작 + 30)
                        {
                            완전한Temp삭제();

                            currentStatus = "리스타트앱";
                            localStepInfo = "정기재시작 요청";

                            _lastRestartDate = now.Date; // 오늘 재시작 했다고 날짜 갱신!
                            _isWaitingForRestart = true; // 무한 대기 모드 진입
                        }
                    }
                }

                if (!string.IsNullOrEmpty(localStepInfo)) stepInfo = localStepInfo;

                // ==========================================================
                // 3. UI 체크박스 상태 업데이트 (철저한 예외 방어)
                // ==========================================================
                try
                {
                    if (this.IsHandleCreated && !this.IsDisposed)
                    {
                        this.Invoke(new Action(() =>
                        {
                            int activeIndex = 0;
                            if (currentStatus == "RUNNING") activeIndex = 1;
                            else if (currentStatus == "ERROR" || currentStatus == "RECORDERR") activeIndex = 2;

                            System.Drawing.Color[] activeColors = {
                                System.Drawing.Color.Blue,
                                System.Drawing.Color.Green,
                                System.Drawing.Color.Red
                            };

                            if (_statusChecks != null)
                            {
                                for (int i = 0; i < _statusChecks.Length; i++)
                                {
                                    if (_statusChecks[i] == null) continue;

                                    bool isChecked = (i == activeIndex);
                                    _statusChecks[i].Checked = isChecked;

                                    if (isChecked)
                                        _statusChecks[i].ForeColor = activeColors[i];
                                    else
                                        _statusChecks[i].ForeColor = System.Drawing.Color.Gray;
                                }
                            }
                        }));
                    }
                }
                catch { }

                // ==========================================================
                // 4. 생명줄 파일 쓰기 (어떤 오류가 나도 무조건 실행됨)
                // ==========================================================
                try
                {
                    string content = $"{now:yyyy-MM-dd HH:mm:ss}|{currentStatus}|{stepInfo}";

                    // 파일이 잠겨있을 수 있으므로 재시도 로직 추가
                    for (int i = 0; i < 3; i++)
                    {
                        try
                        {
                            using (StreamWriter writer = new StreamWriter(path, false, Encoding.GetEncoding(949)))
                            {
                                await writer.WriteAsync(content);
                            }
                            break; // 쓰기 성공 시 루프 탈출
                        }
                        catch (IOException)
                        {
                            await Task.Delay(500); // 0.5초 대기 후 재시도
                        }
                    }
                }
                catch { }

                // 5. 10초 대기
                await Task.Delay(10000, token);
            }
        }

        // ==========================================================================================================
        // Easy약관Plus에서 H2P를 감시하는 로직 (기존 H2P의 구조를 그대로 사용)
        // ==========================================================================================================
        private void StartH2PMonitoring()
        {
            if (h2pMonitorTimer == null)
            {
                h2pMonitorTimer = new System.Windows.Forms.Timer();
                h2pMonitorTimer.Interval = 10000; // 10초마다 체크
                h2pMonitorTimer.Tick += H2PMonitorTimer_Tick;
            }
            h2pMonitorTimer.Start();
        }

        private async void H2PMonitorTimer_Tick(object sender, EventArgs e)
        {
            if (isH2PRestarting) return;

            // H2P가 생성하는 상태 파일 경로
            string statusPath = @"d:\Easy약관Plus\Output\STATUS\H2P감시상태.txt";
            bool needsRestart = false;

            try
            {
                if (File.Exists(statusPath))
                {
                    using (var fs = new FileStream(statusPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    using (var sr = new StreamReader(fs, Encoding.GetEncoding(949)))
                    {
                        string content = sr.ReadToEnd();
                        string[] parts = content.Split('|').Select(x => x.Trim()).ToArray();

                        if (parts.Length >= 2)
                        {
                            DateTime lastTime = DateTime.Parse(parts[0]);
                            TimeSpan diff = DateTime.Now - lastTime;

                            if (diff.TotalSeconds > 180) // 180초 이상 갱신 안됨 -> H2P 멈춤 판단
                            {
                                needsRestart = true;
                            }
                        }
                    }
                }
            }
            catch
            {
                // 파일 읽기 실패 시 무시
            }

            if (needsRestart)
            {
                isH2PRestarting = true;
                h2pMonitorTimer.Stop();

                string 결과 = await 컨트롤러앱실행(상태, "앱실행_재시작");
                if (결과 == "성공")
                {
                    await Task.Delay(10000); // 정상 실행 시 10초 대기 후 감시 재개
                }
                else
                {
                    로그내용 = $"{DateTime.Now:yy.MM.dd HH:mm:ss} [ERROR] ◎진행불가 : H2P APP 샐행을 재시도 합니다.";
                    로그기록(로그내용, 1);
                    await Task.Delay(5000);
                }

                isH2PRestarting = false;
                h2pMonitorTimer.Start();
            }
        }
        private void 삭제Status()
        {
            string 기본Status경로 = @"d:\Easy약관Plus\Output\STATUS\";
            string[] 삭제경로 = new string[] { $"{기본Status경로}앱감시상태.txt", $"{기본Status경로}H2P감시상태.txt" };
            foreach (string 삭제파일 in 삭제경로)
            {
                try
                {
                    if (File.Exists(삭제파일))
                        File.Delete(삭제파일);
                }
                catch (Exception) { }
            }
        }

        public async Task<string> KB손보테이블_설정()
        {
            string 실행여부 = "";
            int 재시도 = 0;
            while (true)
            {
                try
                {
                    SqlDependency.Stop(저장.약관DB);
                    SqlDependency.Start(저장.약관DB);

                    // 최초 한 번 로딩
                    _ = await KB손보개정일저장(0);
                    await 약관KB_메모리로드();

                    // 담보KB 감시 시작
                    await KB손보테이블_감시();
                    await 약관KB테이블_감시();
                }
                catch (Exception exx)
                {
                    string 오류감지 = exx.Message;
                    재시도++;
                    if (재시도 <= 3)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(5));
                        continue;
                    }
                    실행여부 = "실패";
                    throw;
                }
                break;
            }
            return 실행여부;
        }


        /*
            ================================================================================================================================
             ※※※ 슬기로운 설명서 ※※※
            ================================================================================================================================
             01: 증권 + 약관, 02: 약관만, 03: 증권만
             01: 우편,        02: 모바일, 03: 이메일, 04: 로컬

             ※ 병렬처리                : Input폴더에 존재하면 main컴의 H2PControl(Server)에서 파일을 분할해서 sub컴(Client)으로 전송한다.
                                        : Client로 전송된 파일은 List폴더에 존재한다.
                                        : 싱글처리 Input에 파일이 존재해야한다.
             ※ 약관생성(현재상태 상태) : 기본약관PDF생성(상태, 레코드) 후     ==> 상태값 (저장.기본출력폴더\감시\groupKey.단계01) 생성
                                        : 약관작업 완료 후                     ==> 상태값 (출력폴더\\F1~F8\\F1.약관완료 ~ F8.약관완료) 생성
                                                                       
             ※ H2PControl(앱)     ==> 단계01파일검색 : .단계01파일을 감시 후 시작 ==> 상태값 (저장.기본출력폴더\감시\groupKey.단계02) 변경
         
             ※ F1(앱)             ==> .단계02파일을 감시 후 시작    ==> 상태값 (출력폴더\F1\groupKey.F1_시작) 생성
                                   ==> .약관완료 and Hwp파일 = 0     ==> 상태값 (출력폴더\F1\groupKey.F1_종료) 생성
             ※ H2PControl(앱)     ==> 세션별PDF파일 고객단위로 병합 ==> 상태값 (저장.기본출력폴더\감시\groupKey.단계완료) 생성              
             ※ 컨트롤러앱완료대기 ==> .단계완료 파일이 생성 될때까지 대기 상태
        
             ※ 병렬컴 != 1        ==> sub컴은 대기상태 (파일복사,상태값 생성은 H2PControl에서 진행) ==> 병렬컴 = 1은 해당 사항 없음
         
                mainCom복사종료확인==> main컴은 출력폴더\StateFile\groupKey.전송완료 파일이 전송될때 까지 대기
                                       *.전송파일은 클라이언트에서 서버로 PDF를 다 전송 후 전송완료의 의미로 생성
             ※ 개념 : main컴(H2P(Server)), sub컴(H2P(Client))로 구성


             H2P(공통) : 선택약관PDF병합 완료 후 ==> \감시\groupKey.단계완료 생성
         
         
             상태.병렬컴 >=2 이면
         
             H2P(클라) : \Y\*.PDF ==> 서버로 전송
                         PDF전송 후 \StateFile\sub1~sub5.END ==> 서버로 전송
         
             H2P(서버) : \StateFile\*.END 파일대기(상태.병렬컴 - 1)
                        *.END = 상태병렬컴 - 1 같으면 ==> \StateFile\groupKey.전송완료 생성
                        '*맞춤약관작업완료*' ==> 클라이언트 전송
         
             H2P(클라) : *맞춤약관작업완료* 수신 후 ==> \StateFile\모든작업종료.종료 생성
         
             ======================================================================================
         
             현재 맞춤약관App는 작업중인 상태로 우편,모바일 증권합치기 및 북마크 생성단계
        */


        private async void 일하는순서(string 입력, string 출력, string _전달인수)
        {

            // 서식구분 - 01: 증권 + 약관, 02: 약관만, 03: 증권만
            // 발송구분 - 01: 우편,        02: 모바일, 03: 이메일, 04: 로컬

            저장.기본입력폴더 = 입력;
            저장.기본출력폴더 = 출력;
            

            환경설정 설정 = new 환경설정();

            // ==========================================================================================================
            // 00 - 기본설정 : 스레드수 설정, 메인/서버컴퓨터설정, FTP_IP설정
            //      기본설정 값으로 오류발생시 진행불가 
            // ==========================================================================================================

            //  ✅ ① : 기본설정 
            if (await 실행(() => 기본설정(설정, 파라메타), "기본설정") == "실패")  // 진행불가
            {
                진행오류메세지출력();
                MessageBox.Show("🚫진행불가 : 기본설정에 실패 하였습니다.(로그참조).", "기본설정", MessageBoxButtons.OK);
                return;
            }

            _cancellationTokenSource = new CancellationTokenSource();
            // 이 작업은 메인 UI 스레드와 무관하게 백그라운드에서 별도로 돕니다.
            // 경고(CS4014)가 뜰 수 있는데, 의도된 "Fire and Forget"이므로 무시하거나 변수에 할당하면 됩니다.
            _ = Task.Run(() => StartHeartBeatLoop(_cancellationTokenSource.Token));
            StartH2PMonitoring();

            // KB손해보험 - 테이블감시(개정일을 메모리에 저장)
            // KB손보는 개정일이 없고 계약일을 가지고 근사치값을 찾아 실행한다.
            if (_전달인수 != "전체약관")
            {
                if (await 실행(() => KB손보테이블_설정(), "KB손보테이블_설정") == "실패")
                {
                    진행오류메세지출력(); // 진행불가
                    MessageBox.Show("🚫진행불가 : KB손해보험 개정일 설정에 실패 했습니다.", "KB손보개정일오류", MessageBoxButtons.OK);
                    return;
                }
            }

            // ==========================================================================================================
            // 00 - 컨트롤러앱실행 (1/2)
            // ==========================================================================================================

            //  ✅ ② : H2PControl
            string App실행상태 = "";
            if (파라메타 == "재시작")
            {
                App실행상태 = "앱실행_재시작";
            } else {

                if (chk싱글처리.Checked)
                {
                    string 임시컴 = SystemInformation.ComputerName.ToLower();
                    if (임시컴.StartsWith("kbsub") || 임시컴.StartsWith("dzsub"))
                    {
                        App실행상태 = "앱실행_일반";
                    }  else
                    {
                        App실행상태 = "앱실행_싱글처리";
                    }
                }
                else
                {
                    App실행상태 = "앱실행_일반";
                }

            }

            if (_전달인수 != "전체약관")
            {
                if (await 실행(() => 컨트롤러앱실행(상태, App실행상태), "컨트롤러앱실행") == "실패")
                {
                    _ = await 실행(() => 진행불가DB업데이트(상태), "진행불가DB업데이트");
                    MessageBox.Show("🚫진행불가 : 컨트롤러앱실행에 실패 하였습니다.(로그참조).", "컨트롤러앱실행", MessageBoxButtons.OK);
                    return;
                }
            }                       

            if (_전달인수 == "전체약관")
            {
                return;
            }
                        

            while (true)
            {
                //초기화
                string 완료오류 = "";
                저장.상품별세션 = new Dictionary<string, string>();
                레코드s = new List<레코드>();
                bool 다시대기 = false;
                barHwp변환진행.EditValue = 0;
                barpdf결합진행.EditValue = 0;
                담보Merge메인화면_SetMessage?.Invoke(new string[] { "FTP진행", "0", "0", "" });

                집계결과 = new 서식집계();
                상태 = new 현재상태(설정);
                _lastErrorMsg = new string [2];
                bool  처음으로이동 = false;
                try
                {
                    if (await 실행(() => 작업순서이미지설정(), "작업순서이미지설정") == "실패")
                    {
                        진행오류메세지출력();
                    }

                    if (await 실행(() => 이미지읽기(1, "진행후"), "이미지읽기") == "실패")
                    {
                        진행오류메세지출력();
                    }

                    int 데이터검색 = 0;
                    string 이동처리Step = "";
                    다시대기 = false;
                    string 실행여부 = "";

                    if (await 실행(() => 오류작업View(상태), "오류작업View") == "실패")
                    {
                        진행오류메세지출력();
                    }

                    // ==========================================================================================================
                    // 00 - 컨트롤러앱실행 (2/2)
                    // ==========================================================================================================

                    //  ✅ ② : H2PControl
                    App실행상태 = "앱실행_일반";
                    if (await 실행(() => 컨트롤러앱실행(상태, App실행상태), "컨트롤러앱실행") == "실패")
                    {
                        _ = await 실행(() => 진행불가DB업데이트(상태), "진행불가DB업데이트");
                        throw new Exception("ER00_🚫진행불가 : 컨트롤러앱실행, 로그참조");
                    }
                    _ = await 이미지읽기(2, "진행후");


                    string TmpgroupKey = "";
                    while (파라메타 != "재시작" && 파라메타 != "처음시작") //공백일때만
                    {
                        try
                        {
                            // ✅ ③-1 : ListFile탐색
                            // 파일탐색이 2곳에서 이루어진다
                            // 1번째 : (Input폴더) 병렬처리시 H2PControl에서 파일을 병렬cnt만큼 분할해서 서브컴(list폴더)으로 전송한다.
                            // 2번쨰 : (Lint 폴더) 분할 또는 그렇지않은 (싱글처리로 list폴더에 바로 넣는경우) 경우의 파일을 감시한다.
                            //                     @으로 시작되는 파일은 재처리 파일이다.  

                            상태.처리Step = "00";
                            실행여부 = await 실행(() => ListFile탐색(상태, 데이터검색), "ListFile탐색");

                            if (실행여부 == "0") //ListFile이 존재하지 않음 대기중.
                            {
                                데이터검색++;
                                if (데이터검색 % 1 == 0) //10초마다 메세지 출력
                                {
                                    string 문자1 = ">>+++(^< 💦";
                                    int 공백 = 데이터검색 % 10;
                                    string 문자 = 문자1.PadLeft(공백 + 5);
                                    로그내용 = string.Format("{0:yy.MM.dd HH:mm:ss} [핫폴더 감시] INPUT / LIST 폴더 감시 중 : WAITING [{1:000000000}] {2}", DateTime.Now, 데이터검색, 문자);
                                    로그기록(로그내용, 0);
                                }
                                await Task.Delay(TimeSpan.FromSeconds(5));
                                continue;
                            }
                            else if (실행여부 == "1")
                            {
                                상태.처리Step = "01"; // 다음단계인 'ListFiletoTableUplod' 이동
                                _ = await 이미지읽기(3, "진행후");
                                break;
                            } 
                            else if (실행여부.StartsWith("@"))  // 오류가 발생해서 재처리 하는 로직
                            {
                                // ✅ ③-2 : data처리Table검색 (1/2)
                                상태.groupKey = 실행여부.Substring(1);
                                파라메타 = "오류재처리";
                                break;
                            }
                            else if (실행여부 == "실패")
                            {
                                진행오류메세지출력();
                                throw new Exception("ER00_🚫진행불가 : ListFile탐색 오류, 로그참조");
                            }
                            
                        }
                        catch (Exception e1)
                        {
                            에러 = new System.Diagnostics.StackTrace(e1, true);
                            에러코드출력(에러, out int 라인);
                            로그내용 = string.Format("{0:yy.MM.dd HH:mm:ss} [데이터 감시오류] 데이터/테이블 이상 : 라인 -{1}, {2})", DateTime.Now, 라인, e1.Message);
                            로그기록(로그내용, 1);

                            if (e1.Message.Substring(0, 4) == "ER00")
                            {
                                throw;
                            }
                            else {
                                await Task.Delay(TimeSpan.FromSeconds(5));
                                continue;
                            }
                        }
                    }// END while

                    데이터검색 = 0;

                    // ==========================================================================================================
                    // 01 -  ListFile to Table Upload : ListFile을 Table에 업로드한다.
                    //       중복데이터는 제외한다. 
                    //       TemgroupKey 생성 : 담보리스트파일명으로 생성 (250726_103620_#KB_Lngtrm.dat.Re)
                    //       data처리에 제어를 필요한 데이터가 들어가 있다.
                    //       KB손해보험 - 월 2번이상 계정시 개정일이 없어 계약일로 근사값 찾기
                    // ==========================================================================================================

                    Application.DoEvents();
                    
                    //재시작을 하면 모든값이 초기화 된다 '처리중' 재시작은 Table을 읽어 작업을 이어간다.
                    //'완료' 후 재시작은 기본값을으로 초기화 후 다음작업을 한다. 
                    if (파라메타 == "재시작" || 파라메타 == "처음시작")
                    {
                        string 감시폴더 = $"{저장.기본출력폴더}감시\\";
                        TmpgroupKey = "";
                        foreach (FileInfo 감시파일 in new DirectoryInfo(감시폴더).GetFiles("*.단계*"))
                        {
                            TmpgroupKey = System.IO.Path.GetFileNameWithoutExtension(감시파일.Name);
                            break;
                        }
                        
                        if (TmpgroupKey == "")
                        {
                            //완료 후 재시작으로 핫폴더부터 다시 시작한다.
                            파라메타 = "";
                            처음으로이동 = true;
                            continue; //finally문이 있어 continue를 하면 While문으로 이동이 아닌 finally문으로 이동
                                      //그래서 '처음으로이동' 변수를 이용 finally문을 실행하지 않으려고 사용
                        }
                        else
                        {
                            상태.groupKey = TmpgroupKey;
                        }
                    }

                    // data를 업로드 후 구조체에 다시 할당을 한다.
                    if (상태.처리Step == "01" || 파라메타 == "재시작" || 파라메타 == "오류재처리" || 파라메타 == "처음시작") 
                    {
                        // ✅ ④ : FileToTable
                        // 재시작, 오류재처리는 파일업로드가 없다.
                        if (파라메타 != "재시작" && 파라메타 != "오류재처리" && 파라메타 != "처음시작")
                            실행여부 = await 실행(() => FileToTableUp(상태), "FileToTableUp");

                        if (실행여부 == "1" || 파라메타 == "오류재처리" || 파라메타 == "재시작" || 파라메타 == "처음시작")
                        {
                            _ = await 이미지읽기(4, "진행후");

                            // ✅ ③-2 : data처리Table검색 (2/2)
                            실행여부 = await 실행(() => data처리Table검색(상태, 상태.groupKey), "data처리Table검색");

                            if (실행여부 == "실패")
                            {
                                진행오류메세지출력(); // 진행불가, data처리에서 계속 오류가 발생하면 제외하는 로직필요
                                throw new Exception("ER00_🚫진행불가 : data처리Table접속/검색 오류(②), 로그참조");
                            } else if(실행여부 == "0")
                            {
                                //데이터가 없음
                                //데이터 대기로 이동
                                파라메타 = "";
                                처음으로이동 = true;
                                continue;
                            }

                            //상태.처리Step 재정의 , ex)약관생성에서 오류가 나면 약관생성부터 시작이 아닌 Data2구조체 부터 다시 시작한다.
                            if (파라메타 == "오류재처리")
                            {
                                string tmp재정의 = 상태.처리Step;
                                switch (tmp재정의)
                                {
                                    case "02": 
                                    case "03":
                                        상태.처리Step = "02";
                                        break;
                                    case "08":
                                    case "09":
                                        상태.처리Step = "08";
                                        break;
                                }
                            }
                        }
                        else {
                            throw new Exception("ER00");
                        }
                    }
                    
                    // ==========================================================================================================
                    // 02 - 출력에 필요한 폴더를 생성한다.
                    //       상품별 샘플출력은 상품명을 '*'치환한다.
                    //       출력폴더 : 기본출력폴더에서 고객사,날짜,시간,초 등으로 변경됨 (주의요망) - 따라서 파일이 출력되는 위치
                    //       기본출력폴더 : 기본위치 (d:\Easy약관Plus\Output\)
                    //       ListFile의 내용을 뷰에 표시한다.
                    // ==========================================================================================================
                    // ※폴더생성_그리드뷰          - 반환 : 02, 완료
                    // 반드시 실행하는 단계 (data를 읽든, DB에서 데이터를 읽든)
                    if (상태.처리Step == "02")
                    {
                        상태.처리Step = "";
                        상태.처리여부 = "";
                        // ✅ ⑤ : ListFiletoTableUplod
                        if (await 실행(() => Data2구조체(상태,"개별"), "Data2구조체") == "실패")
                        {
                            _ = await 실행(() => 진행불가DB업데이트(상태), "진행불가DB업데이트");
                            throw new Exception("ER00_🚫진행불가 : 폴더생성_GridView, 로그참조");

                        } else {
                            _ = await 이미지읽기(5, "진행후");

                            if (이동처리Step != "")
                                상태.처리Step = 이동처리Step;
                            else
                                상태.처리Step = "03"; // 다음단계인 '약관생성' 이동
                        }
                    }


                    // ======================================== [ ** 병렬처리 시작 ** ] ========================================


                    // =========================================================================================================
                    // 03 - 기본_H,T,S,I, 목차 파일 저장 
                    // 상품별 해당 파일을 미리저장하여 DB 과접속을 사전에 줄일려는 목적임
                    // =========================================================================================================

                    //==========================================================================================================
                    // 03 -특별담보, 목차, 별표 생성
                    //==========================================================================================================
                    // ※약관생성                  - 반환 : 03, 완료

                    bool 단계03생성여부 = true;
                    if (상태.처리Step == "03")
                    {

                        단계진행문구출력("03");

                        담보Merge메인화면event = new string[10];
                        담보Merge메인화면event[0] = "담보Merge소요시간";
                        담보Merge메인화면event[1] = "HwpMerge";
                        담보Merge메인화면event[2] = "선택담보Merge시작";
                        담보Merge메인화면_SetMessage?.Invoke(담보Merge메인화면event);

                        상태.처리Step = "";

                        _ = await 이미지읽기(6, "진행후");
                        // ✅ ⑥-1 : 약관생성

                        실행여부 = await 실행(() => 약관생성(상태), "약관생성");

                        단계03생성여부 = false;

                        if (실행여부 == "성공")
                        {
                            상태.처리Step = "04"; // 다음단계인 '' 이동
                        }
                        else
                        {
                            _ = await 실행(() => 진행불가DB업데이트(상태), "진행불가DB업데이트");
                            throw new Exception("🚫진행불가 : 약관생성 오류, 로그참조");
                        }


                    }
                    //H2PControl 
                    //04: PDF변환작업
                    //05: PDF병합작업
                    if (단계03생성여부 && 파라메타 == "오류재처리")
                    {
                        string 기본감시폴더 = $@"{저장.기본출력폴더}감시\";
                        string 감시폴더 = $"{기본감시폴더}{상태.groupKey}.단계03";
                        string 출력내용 = $"{상태.고객사}|{상태.groupKey}|{상태.출력폴더}";
                        File.WriteAllText(감시폴더, 출력내용, Encoding.GetEncoding(949));
                        //생성이유 : 오류재처리시 H2P앱에서 '단계01파일검색_파일분리'에서 약관생성(03|완료) 이후 이면 
                        //           '단계01'(약관생성에서 생성됨) 생성되지 않아 무한 대기 상태
                    }


                    // ==========================================================================================================
                    // 04,05 - H2PControl제어
                    //         F1 ~ F8 : Hwp to PDF 변환 후 개별약관별로 PDF병합  
                    // ==========================================================================================================

                    if (상태.처리Step == "04")
                    {
                        // ✅ ⑥-2 : 컨트롤러앱완료대기
                        //            H2PControl이 개별약관병합이 완료 될때까지 대기한다.
                        //            완료가 되면 H2PControl이 *.단계완료 파일을 생성한다.(main, sub 공통)
                        if (await 실행(() => 컨트롤러앱완료대기(상태), "컨트롤러앱완료대기") != "성공")
                        {
                            로그내용 = string.Format("{0:yy.MM.dd HH:mm:ss} [ERROR]컨트롤러앱 완료 오류 ", DateTime.Now);
                            로그기록(로그내용, 1);
                        }
                        _ = await 이미지읽기(7, "진행후");
                        //main컴, sub1~sub3컴까지 모두 해당
                        if (await 실행(() => 병렬처리DBUpdate(상태, "개별1", null), "병렬처리DBUpdate(개별1)") == "성공")
                        {
                            상태.처리Step = "05";
                        }
                        else {
                            _ = await 실행(() => 진행불가DB업데이트(상태), "진행불가DB업데이트");
                            throw new Exception("🚫진행불가 : 병렬처리DBUpdate(개별1) 오류, 로그참조");
                        }
                        _ = await 이미지읽기(8, "진행후");
                    }

                    // ==========================================================================================================
                    // 05 - 서브컴에서 메인컴으로 개별맞춤약관 이동 (H2PControl (서버 <== 클라이언트) 이용)
                    // ==========================================================================================================
                    if (상태.처리Step == "05") {
                        //병렬처리, main컴
                        if (상태.병렬컴 != 1 && 상태.상태컴퓨터 == "main")
                        {
                            if (await 실행(() => mainCom복사종료확인(상태), "mainCom복사종료확인(복사)") == "실패")
                            {
                                _ = await 실행(() => 진행불가DB업데이트(상태), "진행불가DB업데이트");
                                throw new Exception("mainCom복사종료확인(복사) 오류, 로그참조");
                            }

                        }
                        상태.처리Step = "06";
                    }

                    담보Merge메인화면event = new string[10];
                    담보Merge메인화면event[0] = "담보Merge소요시간종료";
                    담보Merge메인화면_SetMessage.Invoke(담보Merge메인화면event);



                    // ======================================== [ ** 병렬처리 종료 **  ] ========================================
                    // 

                    // ==========================================================================================================
                    // 06 - 증권 + 선택약관 합치기(현재:약관만은 모바일/이메일만 처리한다.)
                    //      목차쪽번호 출력(증권+약관중 약관부분만 출력한다.
                    //      Bookmarks 생성
                    //      모바일(M) 데이타를 가져온다.(성공한 데이타만 가지고 있음)
                    //      증권약관합치기(개별PDF작업)은 ListFile 중심으로 완료/오류만 존재 처리후 '대기'는 없음
                    // ==========================================================================================================

                    if (상태.상태컴퓨터 == "main")
                    {

                        _ = await 실행(() => Data2구조체(상태, "전체"), "Data2구조체");

                        if (상태.처리Step == "06")
                        {
                            단계진행문구출력("06");

                            barpdf결합진행.EditValue = 0;
                            담보Merge메인화면event = new string[10];
                            담보Merge메인화면event[0] = "증권Merge소요시간";
                            담보Merge메인화면event[1] = "PDFMerge";
                            담보Merge메인화면event[2] = "증권+선택약관Merge";
                            담보Merge메인화면_SetMessage.Invoke(담보Merge메인화면event);



                            if (await 실행(() => 증권약관합치기(상태), "증권약관합치기") == "실패")
                            {
                                _ = await 실행(() => 진행불가DB업데이트(상태), "진행불가DB업데이트");
                                throw new Exception("증권약관합치기 오류, 로그참조");
                            }

                            _ = await 이미지읽기(9, "진행후");

                            if (await 실행(() => 병렬처리DBUpdate(상태, "개별2", null), "병렬처리DBUpdate(개별2)") == "실패")
                            {
                                _ = await 실행(() => 진행불가DB업데이트(상태), "진행불가DB업데이트");
                                throw new Exception("병렬처리DBUpdate(개별2) 오류, 로그참조");
                            }


                            담보Merge메인화면event = new string[10];
                            담보Merge메인화면event[0] = "증권Merge소요시간종료";
                            담보Merge메인화면_SetMessage.Invoke(담보Merge메인화면event);

                            상태.처리Step = "07";
                        }

                        //                                      여기까지는 필수 실행 사항 ↑
                        // ================================================================================================================================================
                        //                                      아래부터는 선택 실행 사항 ↓

                        집계결과 = new 서식집계
                        {
                            우편cnt = 레코드s.Count(x => x.발송구분 == "01" || x.발송구분 == "1"),
                            모바일cnt = 레코드s.Count(x => x.발송구분 == "02" || x.발송구분 == "2"),
                            이메일cnt = 레코드s.Count(x => x.발송구분 == "03" || x.발송구분 == "3"),
                            로컬cnt = 레코드s.Count(x => x.발송구분 == "04" || x.발송구분 == "4"),

                        };

                        // ==========================================================================================================
                        // 07 - 우편발송 일때 우편폴더에 있는 파일을 출력에 용이하게 1개로 묶는다.
                        //      현재상품별로 되어 있으나 추후 장수별로 묶어야 함
                        //      우편작업부터는 선택사항으로 값이 '완료/오류/대기' 구분됨  
                        // ==========================================================================================================
                        if (상태.처리Step == "07" && 집계결과.우편cnt > 0 && chk우편.Checked)
                        {
                            if (await 실행(() => 우편출력_파일병합(상태), "우편출력_파일병합") == "성공")
                            {
                                if (await 실행(() => 병렬처리DBUpdate(상태, "개별3", 집계결과), "병렬처리DBUpdate(개별3)") == "실패")
                                {

                                }
                            }

                            상태.처리Step = "08";
                        }

                        if (상태.처리Step != "09")  // 오류 재시도 - Table검색으로 나온값으로 바로 모바일로 이동한다.
                        {
                            상태.처리Step = "08";
                        }

                        // ※※※※※ 주의사항 KB손해보험 전용 PC와 맞춤약관PC를 반드 구분할것 ※※※※※
                        bool FTPDB작업 = false;
                        if (((저장.사용컴퓨터 == "main" && 상태.고객사 == "KB") ||
                            (저장.사용컴퓨터 == "db_main" && 상태.고객사 == "DB") ||
                            (저장.사용컴퓨터 == "dzmain" && 상태.고객사 == "DZ") ||
                            (저장.사용컴퓨터 == "kbmain" && 상태.고객사 == "KB")
                            ) && chkFTP.Checked
                           )
                        {
                            FTPDB작업 = true;

                        } else
                        {
                            집계결과.모바일cnt = -999;
                        }


                        // ==========================================================================================================
                        // 08 - FTP File Upload 
                        // 09 - 모바일 DB Upload     
                        // ==========================================================================================================
                        if ((상태.처리Step == "08" || 상태.처리Step == "09") && FTPDB작업 && 집계결과.모바일cnt > 0)
                        {
                            if (상태.처리Step == "08")
                            {

                                담보Merge메인화면event = new string[10];
                                담보Merge메인화면event[0] = "FTP소요시간";
                                담보Merge메인화면event[1] = "FTPUpload";
                                담보Merge메인화면event[2] = "FTP 파일 업로드";
                                담보Merge메인화면_SetMessage.Invoke(담보Merge메인화면event);

                                //SFTP
                                if (저장.사용컴퓨터 == "kbmain")
                                {
                                   
                                    if (await 실행(() => FTP_FILE_UPLOAD2(상태), "FTP_FILE_UPLOAD") == "성공")
                                    {
                                        if (await 실행(() => 병렬처리DBUpdate(상태, "개별4", 집계결과), "병렬처리DBUpdate(개별4)") == "실패")
                                        {
                                            _ = await 실행(() => 진행불가DB업데이트(상태), "진행불가DB업데이트");
                                            throw new Exception("병렬처리DBUpdate(개별4) 오류, 로그참조");
                                        }
                                        _ = await 이미지읽기(10, "진행후");
                                    }
                                } else  {
                                    //삭제예정
                                    if (await 실행(() => FTP_FILE_UPLOAD(상태), "FTP_FILE_UPLOAD") == "성공")
                                    {
                                        if (await 실행(() => 병렬처리DBUpdate(상태, "개별4", 집계결과), "병렬처리DBUpdate(개별4)") == "실패")
                                        {
                                            _ = await 실행(() => 진행불가DB업데이트(상태), "진행불가DB업데이트");
                                            throw new Exception("병렬처리DBUpdate(개별4) 오류, 로그참조");
                                        }
                                        _ = await 이미지읽기(10, "진행후");
                                    }
                                }

                                상태.처리Step = "09";

                                담보Merge메인화면event = new string[10];
                                담보Merge메인화면event[0] = "FTP소요시간종료";
                                담보Merge메인화면_SetMessage.Invoke(담보Merge메인화면event);
                            }

                            if (상태.처리Step == "09" && FTPDB작업 && 집계결과.모바일cnt > 0)
                            {
                                if (await 실행(() => 모바일데이터업로드(상태), "모바일데이터업로드") == "성공")
                                {
                                    if (await 실행(() => 병렬처리DBUpdate(상태, "개별5", 집계결과), "병렬처리DBUpdate(개별5)") == "실패")
                                    {
                                        _ = await 실행(() => 진행불가DB업데이트(상태), "진행불가DB업데이트");
                                        throw new Exception("병렬처리DBUpdate(개별5) 오류, 로그참조");
                                    }
                                    _ = await 이미지읽기(11, "진행후");
                                }
                            }

                        }

                        // 최종 작업관련해서 업데이트를 한다.
                        // data처리 Table
                        // 값 : 실패, 성공(완료, 오류)
                        완료오류 = await 실행(() => 병렬처리DBUpdate(상태, "일괄", 집계결과), "병렬처리DBUpdate(일괄)");  //<======= 일괄
                        if (완료오류 == "실패")
                        {
                            _ = await 실행(() => 진행불가DB업데이트(상태), "진행불가DB업데이트");
                            throw new Exception("병렬처리DBUpdate(일괄) 오류, 로그참조");
                        }

                        // DB일괄 정리 
                        // 1) 완료 : data처리, data 삭제 ==> Err_data, Err_data처리 에서도 삭제(재작업시 존재함)
                        // 2) 오류 : data처리, data 이동 ==> Err_data, Err_data처리


                    }
                }
                catch (Exception e1)
                {
                    _lastErrorMsg[0] = e1.Message;
                    await Task.Delay(TimeSpan.FromSeconds(5));
                    string 에러코드 = e1.Message.Substring(0, 4); // ListFile 업로드 전
                    if (에러코드 == "ER00" || 에러코드 == "ER01")
                    {
                        다시대기 = true;
                        await Task.Delay(TimeSpan.FromSeconds(5));
                    }
                    else {
                        에러 = new System.Diagnostics.StackTrace(e1, true);
                        에러코드출력(에러, out int 라인);
                        로그내용 = string.Format("{0:yy.MM.dd HH:mm:ss} [오류] {1} (메소드 : {2}, Line : {3})", DateTime.Now, e1.Message, 메소드명, 라인);
                        로그기록(로그내용, 1);
                    }

                }
                finally
                {
                    if (!처음으로이동)
                    {
                        try
                        {
                            if (!다시대기)
                            {
                                //main만 오류가 발생하면 ==> Err_data처리, Err_data로 이동
                                if (상태.상태컴퓨터 == "main")
                                {
                                    _ = await 실행(() => 완료오류처리(상태, 완료오류), "완료오류처리");
                                    //현재 H2P는 main컴이 작업이 완료 될때까지 대기하고 있다

                                }
                                else if (상태.병렬컴 >= 2 && 상태.상태컴퓨터 != "main") //클라이언트만 해당
                                {
                                    _ = await sub컴종료(상태);
                                }

                                _ = await 실행(() => 작업완료후삭제(상태), "작업완료후삭제");
                            }

                            try
                            {
                                string 감시폴더 = $"{저장.기본출력폴더}감시\\";
                                foreach (FileInfo 감시파일 in new DirectoryInfo(감시폴더).GetFiles("*.*"))
                                {
                                    try
                                    {
                                        감시파일.Delete();
                                    }
                                    catch
                                    {
                                        // 삭제 실패한 파일은 무시하고 다음 파일 삭제 계속 진행
                                    }
                                }

                                if (상태.상태컴퓨터 == "main")
                                {
                                    ////// 모든작업이 완료
                                    string 작업완료파일 = $"{저장.기본출력폴더}감시\\{상태.groupKey}.main작업완료";
                                    File.WriteAllText(작업완료파일, "main컴 모든작업 완료", Encoding.GetEncoding(949));
                                }
                            }
                            catch (Exception ex)
                            {
                                this.BeginInvoke(new Action(() =>
                                {
                                    로그내용 = string.Format("{0:yy.MM.dd HH:mm:ss} [ERROR] 감시폴더 정리 및 완료신호 생성 실패 : {1}", DateTime.Now, ex.Message);
                                    로그기록(로그내용, 1);
                                }));
                            }

                            //모든작업 완료하면 종료파일을 생성한다.
                            
                            string 작업완료종료 = $"{상태.출력폴더}({DateTime.Now:yy.MM.dd})EOF.EOF";
                            File.WriteAllText(작업완료종료, $"{DateTime.Now:yy.MM.dd|HH.mm.ss fffff}", Encoding.GetEncoding(949));

                        }
                        catch (Exception e1)
                        {
                            _lastErrorMsg[0] = e1.Message;
                            에러 = new System.Diagnostics.StackTrace(e1, true);
                            에러코드출력(에러, out int 라인);
                            로그내용 = string.Format("{0:yy.MM.dd HH:mm:ss} [오류] {1} (메소드 : {2}, Line : {3})", DateTime.Now, e1.Message, 메소드명, 라인);
                            로그기록(로그내용, 1);
                            await Task.Delay(TimeSpan.FromSeconds(5));
                        }
                    }
                    파라메타 = ""; //초기화
                }
            }
        }

        private async Task<string> sub컴종료 (현재상태 상태)
        {
            string 실행여부 = "성공";

            로그내용 = $"{DateTime.Now:yy.MM.dd HH:mm:ss} [{상태.상태컴퓨터}] 작업종료 대기중";
            로그기록(로그내용, 1);
            while (true)
            {
                try
                {
                    // H2p(서버)에서 PDF수신완료 후 H2P(클라이언트)에 '*맞춤약관작업완료*' 전송  클라이언트는 수신 후 '모든작업종료.종료' 생성
                    string 작업완료 = $"{상태.출력폴더}StateFile\\모든작업종료.종료";

                    if (!new FileInfo (작업완료).Exists)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(3));
                        continue;
                    } else
                    {
                        로그내용 = $"{DateTime.Now:yy.MM.dd HH:mm:ss} [{상태.상태컴퓨터}] 작업종료 완료";
                        로그기록(로그내용, 1);
                        break;
                    }
                }
                catch (Exception)
                {
                }
            }

            return 실행여부;
        }


        private async Task<string> 작업완료후삭제(현재상태 상태)
        {
            int 재시도 = 0;
            string 실행여부 = "성공";
            while (true)
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(상태.출력폴더))
                    {
                        로그기록($"{DateTime.Now:yy.MM.dd HH:mm:ss} [ERROR] 파일삭제 실패 : 상태.출력폴더 경로가 비어있습니다.", 0);
                        return "실패";
                    }

                    var di = new DirectoryInfo(상태.출력폴더);
                    if (!di.Exists) return 실행여부;

                    var 폴더파일 = di.EnumerateFileSystemInfos();
                    foreach (var 삭제 in 폴더파일)
                    {
                        if ((삭제.Attributes & FileAttributes.Directory) == FileAttributes.Directory)
                        {
                            if (삭제.Name == "@맞춤증권" && 상태.상태컴퓨터 == "main") //sub컴은 모든 데이터 삭제
                            {
                                string bookmarks경로 = System.IO.Path.Combine(삭제.FullName, "bookmarks");

                                try
                                {
                                    if (new DirectoryInfo(bookmarks경로).Exists)
                                        Directory.Delete($"{삭제.FullName}\\bookmarks", true);
                                }
                                catch (Exception){}
                                
                            }
                            else
                            {
                                try
                                {
                                    if (삭제.Exists)
                                        Directory.Delete(삭제.FullName, true);
                                }
                                catch (Exception){}
                            }
                        }
                        else  {
                            try
                            {
                                if (삭제.Exists)
                                    삭제.Delete();
                            }
                            catch (Exception){}
                        }
                    }
                    break;
                }
                catch (Exception ex)
                {
                    재시도++;
                    if (재시도 <= 3)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(5));
                        continue;
                    }
                    로그내용 = $"{DateTime.Now:yy.MM.dd HH:mm:ss} [ERROR] 파일삭제 실패 (경로: {상태.출력폴더}) : {ex.Message}";
                    로그기록(로그내용, 0);

                    실행여부 = "실패";
                    throw;
                }
            }
            return 실행여부;
        }

        // =======================================================================================================================================
        //                                                     [ 프로그램 분리]
        // =======================================================================================================================================



        // ==========================================================================================================
        // 00 -01 기본화면설정 : 스레드갯수 설정
        // ==========================================================================================================
        private async Task<string> 기본화면설정()
        {
            await Task.Delay(TimeSpan.FromSeconds(0.01));
            string 실행여부 = "성공";
            try
            {
                string[] tmp스레드수 = new string[] { "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13", "14", "15", "16" };
                cob스레드수.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.Standard;
                cob스레드수.Properties.Items.AddRange(tmp스레드수);

                cob스레드수.Text = "8";
                cob스레드수.EditValue = "8";
                    
                barHwp변환진행.Properties.ShowTitle = true;    // 값표시
                barHwp변환진행.Properties.PercentView = true;  // %로 표시 
                barHwp변환진행.Properties.Maximum = 100;
                barHwp변환진행.Properties.Minimum = 0;
                barHwp변환진행.Properties.Step = 1;
                barHwp변환진행.Properties.ProgressViewStyle = DevExpress.XtraEditors.Controls.ProgressViewStyle.Broken;
                barHwp변환진행.EditValue = 0;

                barpdf결합진행.Properties.ShowTitle = true;    // 값표시
                barpdf결합진행.Properties.PercentView = true;  // %로 표시 
                barpdf결합진행.Properties.Maximum = 100;
                barpdf결합진행.Properties.Minimum = 0;
                barpdf결합진행.Properties.Step = 1;
                barpdf결합진행.Properties.ProgressViewStyle = DevExpress.XtraEditors.Controls.ProgressViewStyle.Broken;
                barpdf결합진행.EditValue = 0;
            }
            catch (Exception)
            {
                실행여부 = "실패";
                throw;
            }
            return 실행여부;
            
        }
        // DrawItem 이벤트 핸들러 (DevExpress 전용 인자 타입)

        private void lst로그_DrawItem(object sender, DevExpress.XtraEditors.ListBoxDrawItemEventArgs e)
        {
            if (e == null || e.Index < 0) return;

            string text = e.Item != null ? e.Item.ToString() : string.Empty;

            // 기본 스타일 (명시적으로 System.Drawing 네임스페이스 사용)
            e.Appearance.BackColor = System.Drawing.Color.FromArgb(190, 190, 190);//페더 그레이
            e.Appearance.ForeColor = System.Drawing.Color.Black;
            e.Appearance.Font = new Font(e.Appearance.Font, System.Drawing.FontStyle.Regular);

            // "오류" 포함시 강조
            if (text.IndexOf("[오류]", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                e.Appearance.BackColor = System.Drawing.Color.Red;
                e.Appearance.ForeColor = System.Drawing.Color.White;
                e.Appearance.Font = new Font(e.Appearance.Font, System.Drawing.FontStyle.Bold);

            } else if (text.IndexOf ("[처리단계 ###") >=0) {
                e.Appearance.BackColor = System.Drawing.Color.Blue;
                e.Appearance.ForeColor = System.Drawing.Color.White;
            }
            else if (text.IndexOf("[KB손보]") >= 0)
            {
                e.Appearance.BackColor = System.Drawing.Color.Black;
                e.Appearance.ForeColor = System.Drawing.Color.Yellow;

            }
            else if (text.IndexOf("[약관생성오류]") >= 0)
            {
                e.Appearance.BackColor = System.Drawing.Color.Indigo;
                e.Appearance.ForeColor = System.Drawing.Color.Yellow;

            }
            else if (text.IndexOf("[작업처리 상황]") >= 0 )
            {
                e.Appearance.BackColor = System.Drawing.Color.Orange;
                e.Appearance.ForeColor = System.Drawing.Color.White;
                e.Appearance.Font = new Font(e.Appearance.Font, System.Drawing.FontStyle.Bold);
            }
            else if (text.IndexOf("[상세오류") >= 0 || text.IndexOf("[작업건수") >= 0 || text.IndexOf("[상세오류") >= 0)
            {
                e.Appearance.BackColor = System.Drawing.Color.Green;
                e.Appearance.ForeColor = System.Drawing.Color.White;
                e.Appearance.Font = new Font(e.Appearance.Font, System.Drawing.FontStyle.Bold);
            }
            else if (text.IndexOf(">>+++(^<") >= 0)
            {
                e.Appearance.BackColor = System.Drawing.Color.Purple;
                e.Appearance.ForeColor = System.Drawing.Color.White;

            }
        }
   
        // ==========================================================================================================
        // 00 -02 기본설정 : 메인/서버컴퓨터설정, FTP_IP설정
        // ==========================================================================================================
        private async Task<string> 기본설정(환경설정 설정, string _파라메타)
        {
            try
            {
                // =======================================================
                // 1. 감시폴더 정리
                // =======================================================
                if (_파라메타 == "재시작" || _파라메타 == "처음시작")
                {
                    string 감시폴더 = $@"{저장.기본출력폴더}감시\";
                    if (Directory.Exists(감시폴더))
                    {
                        DirectoryInfo di = new DirectoryInfo(감시폴더);
                        foreach (FileInfo 삭제파일 in di.GetFiles("*.*"))
                        {
                            try { if (삭제파일.Extension.ToLower() != ".단계02") 삭제파일.Delete(); }
                            catch { /* 사용 중인 파일은 무시 */ }
                        }
                    }
                }

                // =======================================================
                // 3. NetworkManager 
                // =======================================================
                string 결과 = await NetworkManager.공통설정파일_로드();
                if (결과 == "실패")
                {
                    MessageBox.Show("설정 파일(localDB.txt) 로드에 실패했습니다.", "설정 오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return "실패";
                }

                // =======================================================
                // 3. UI 종속적인 특수 세팅 (Merge 폼만의 싱글 모드 덮어쓰기)
                // =======================================================
                bool isSingleMode = false;
                if (chk싱글처리.InvokeRequired)
                    chk싱글처리.Invoke(new Action(() => isSingleMode = chk싱글처리.Checked));
                else
                    isSingleMode = chk싱글처리.Checked;

                // 글로벌 기본값(내 컴퓨터명)은 이미 NetworkManager가 세팅해 둠.
                // 싱글 모드일 때만 글로벌 변수를 덮어씀 (예외 처리)
                if (isSingleMode)
                {
                    string 임시컴 = SystemInformation.ComputerName.ToLower();
                    if (임시컴.StartsWith("kbsub")) 저장.사용컴퓨터 = "kbmain";
                    else if (임시컴.StartsWith("sub")) 저장.사용컴퓨터 = "main";
                    else if (임시컴.StartsWith("dzsub")) 저장.사용컴퓨터 = "dzmain";
                }



                // =======================================================
                // 4. 로드된 결과값을 UI 및 '설정' 객체에 반영
                // =======================================================
                string 컴유형 = "";
                if (저장.설정값.TryGetValue($"병렬{저장.사용컴퓨터}", out string tmp컴유형))
                    컴유형 = tmp컴유형;

                switch (컴유형)
                {
                    case "sub":
                        설정.상태컴퓨터 = 저장.사용컴퓨터.Substring(저장.사용컴퓨터.Length - 4, 4);
                        int 인수 = 설정.상태컴퓨터.IndexOf("sub") + 3;
                        if (int.TryParse(설정.상태컴퓨터.Substring(인수), out int 숫자))
                            설정.처리구분 = string.Format("{0:00}", 숫자);
                        break;
                    default:
                        설정.상태컴퓨터 = "main";
                        설정.처리구분 = "00";
                        break;
                }

                // =======================================================
                // 5. DB 접속 정보 세팅 및 UI 컨트롤 업데이트
                // =======================================================
                if (저장.설정값.TryGetValue($"접속{저장.사용컴퓨터}", out string 접속) && !isSingleMode)
                {
                    설정.dataTable접속 = 접속;
                }
                else
                {
                    // [안전성] 딕셔너리에 키가 없어도 뻗지 않고 TryGetValue로 방어
                    if (저장.설정값.TryGetValue("접속LOCALDB", out string localDb))
                        설정.dataTable접속 = localDb;
                    else
                        throw new Exception("localDB.txt 파일에 '접속LOCALDB' 설정값이 존재하지 않습니다.");
                }

                if (저장.설정값.TryGetValue($"접속{저장.사용컴퓨터}", out string 접속1))
                {
                    설정.URLTable접속 = 접속1;
                }


                return "성공";
            }
            catch (Exception ex)
            {
                // 예상치 못한 에러가 나더라도 프로그램이 죽지 않고 메시지를 띄움
                MessageBox.Show($"기본 설정 중 오류가 발생했습니다.\n\n{ex.Message}", "기본 설정 오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return "실패";
            }
        }

        // ==========================================================================================================
        // 01 - 01 ListFile탐색 : 컴퓨터네임이 'main'이면 입력폴더에서 ListFile을 읽어 입력한다. 
        //                        DB손해보험(모바일)을 제외한 고객사는 여러개의 ListFile이 존재하면 하나로 묶어처리
        // ==========================================================================================================
        private async Task<string> ListFile탐색(현재상태 상태, int 데이터검색)
        {
            string 실행여부;

            if (데이터검색 == 0)
            {
                로그내용 = string.Format("===============================================================================================");
                로그기록(로그내용, 0);
                로그내용 = string.Format("{0:yy.MM.dd HH:mm:ss} [핫폴더 시작] 맞춤약관 작업시작 : === INPUT / LIST 폴더 감시를 시작합니다. ===", DateTime.Now);
                로그기록(로그내용, 0);
                로그내용 = string.Format("===============================================================================================");
                로그기록(로그내용, 0);

            }
            int 재시도 = 0;
            while (true)
            {
                try
                {

                    //재처리는 @ + PK(groupKey)로 파일이 생성된다.
                    DirectoryInfo 파일목록 = new DirectoryInfo(저장.기본입력폴더);
                    foreach (FileInfo 파일 in 파일목록.GetFiles("@*.*"))
                    {
                        실행여부 = 파일.Name;
                        파일.Delete();
                        return 실행여부;
                    }
                    
                    int 작업파일갯수 = 파일목록.GetFiles("*.*").Count();
                    if (작업파일갯수 == 0) {
                        실행여부 = "0";
                        break;
                    }

                    List<string> tmp작업파일 = new List<string>();
                    string tmp고객사 = "";
                    bool 처음저장 = false;

                    string 이동폴더 = "";
                    string 이동파일 = "";
                    이동폴더 = 폴더생성(저장.기본입력폴더, "@완료");
                    이동폴더 = 폴더생성(이동폴더, "@파일규칙오류");
                    foreach (FileInfo 파일 in 파일목록.GetFiles("*.*"))
                    {
                        if (파일.Name.IndexOf('#') != -1) // 고객사코드는 반드시 #으로 시작한다.
                            상태.고객사 = 파일.Name.Substring(파일.Name.IndexOf('#') + 1, 2);
                        else
                        {
                            이동파일 = string.Format("{0}{1}", 이동폴더, 파일.Name);
                            bool 복사여부 = 복사_이동_삭제("복사_삭제", 파일.FullName, 이동파일);
                            if (복사여부 == false)
                                throw new Exception($"[ERROR] 파일복사/삭제 오류 : { 파일.FullName}");

                            로그내용 = string.Format("{0:yy.MM.dd HH:mm:ss} [ERROR] 파일네이밍룰 오류 : 고객사코드는 반드시 #으로 시작 : {1}", DateTime.Now, 파일.Name);
                            로그기록(로그내용, 1);
                            continue;
                        }

                        // 처음파일을 기준으로 동일한 고객사를 묶어 한꺼번에 처리 한다.
                        if (처음저장 == false)
                        {
                            tmp고객사 = 상태.고객사;
                            처음저장 = true;
                        }

                        if (tmp고객사 == 상태.고객사)
                        {
                            tmp작업파일.Add(파일.FullName);
                            if (상태.고객사 == "DB") // DB손해보험 파일명을 기본키값으로 사용해서 Merge를 하지 않는다.(모바일)
                                break;
                        }
                    }
                    if (tmp작업파일.Count == 0)
                        throw new Exception("[ERROR] 파일네이밍룰 오류 : ListFile에 '#고객사명'이 존재하지 않습니다.");

                    상태.고객사 = tmp고객사;  // 여러고객사 파일 들어 오면 처리하는 고객사로 원위치 한다.
                    string 처리날짜 = string.Format("{0:yyMMdd}", DateTime.Now);

                    // @완료 폴더생성
                    // 하위폴더가 없으면 강제로 생성한다.
                    string 임시이동 = $"@완료\\{상태.고객사}\\{DateTime.Now:yyyy.MM}\\{DateTime.Now:dd}\\";
                    이동폴더 = 폴더생성(저장.기본입력폴더, 임시이동);

                    bool 병렬처리여부 = System.IO.Path.GetFileNameWithoutExtension(tmp작업파일[0]).IndexOf ("[M") != -1 ? true : false;
                    FileInfo tmp파일 = new FileInfo(tmp작업파일[0]);
                    string 병렬확장자 = tmp파일.Extension.ToLower();

                    // .main, .sub1 ~ .sub5이 존재하면 input폴더 ==> list폴더로 이동한 파일 ex) 251106_1345206638_#KB_Lngtrm_Coorp_Plcy_Clause_SEND_20250918.dat.main
                    string PK중복방지 = "";
                    if (병렬처리여부)
                    {
                        PK중복방지 = "";
                    } else
                    {
                        PK중복방지 = string.Format("{0:yyMMdd_HHmmssFFFF}", DateTime.Now);
                        병렬확장자 = "";
                    }

                    string 처리파일 = "";

                    //삭제2 : 리스트파일 삭제(이동)
                    if (tmp작업파일.Count == 1)
                    {
                        //바로 파일을 끌어오면 쓰기도중에 파일을 가져온다.
                        long 파일용량1 = new FileInfo(tmp작업파일[0]).Length;
                        while (true)
                        {
                            await Task.Delay(1000);
                            long 파일용량2 = new FileInfo(tmp작업파일[0]).Length;
                            if (파일용량1 != 파일용량2)
                                파일용량1 = 파일용량2;
                             else
                                break;
                        }

                        처리파일 = 병렬확장자 != "" ? new FileInfo(tmp작업파일[0]).Name.Replace(병렬확장자, "") : $"_{new FileInfo(tmp작업파일[0]).Name}";
                        이동파일 = string.Format("{0}{1}{2}.Re", 이동폴더, PK중복방지, 처리파일);
                        bool 복사여부 = 복사_이동_삭제("복사_삭제", tmp작업파일[0], 이동파일);
                        if(복사여부 == false )
                            throw new Exception($"[ERROR] 파일복사/삭제 오류 : 리스트파일 이동에 오류발생 - {tmp작업파일[0]}");
                    }
                    else
                    {
                        // 여러개의 파일 존재하면 대표파일명을 새롭게 작성한다.
                        처리파일 = string.Format("#{0}_{1}_Merge({2:000}EA).lst", 상태.고객사, PK중복방지,tmp작업파일.Count);

                        List<string> 출력내용 = new List<string>();
                        int 파일cnt = 0;

                        long 파일용량1 = 0;
                        foreach (string 파일경로 in tmp작업파일)
                            파일용량1 += new FileInfo(파일경로).Length;

                        while (true)
                        {
                            long 파일용량2 = 0;
                            await Task.Delay(1000);
                            
                            foreach (string 파일경로 in tmp작업파일)
                                파일용량2 += new FileInfo(파일경로).Length;

                            if (파일용량1 != 파일용량2)
                                파일용량1 = 파일용량2;
                            else
                                break;
                        }

                        foreach (string 파일경로 in tmp작업파일)
                        {
                            string[] 파일읽기 = File.ReadAllLines(파일경로, Encoding.GetEncoding(949));
                            출력내용.AddRange(파일읽기);

                            string 대상경로 = string.Format("{0}{1}_{2}", 이동폴더, PK중복방지, new FileInfo(파일경로).Name);
                            bool 복사여부 = 복사_이동_삭제("복사_삭제", 파일경로, 대상경로);
                            if (복사여부 == false)
                                throw new Exception($"파일복사/삭제 오류 : {파일경로}");

                            파일cnt++;
                            로그내용 = string.Format("{0:yy.MM.dd HH:mm:ss} [리스트파일병합] ListFile병합시작 : {1:000} - {2} ", DateTime.Now, 파일cnt, new FileInfo(파일경로).Name);
                            로그기록(로그내용, 1);
                        }

                        이동파일 =  string.Format("{0}{1}_{2}.Re", 이동폴더, PK중복방지, 처리파일);
                        File.WriteAllLines(이동파일, 출력내용, Encoding.GetEncoding(949));

                        로그내용 = string.Format("{0:yy.MM.dd HH:mm:ss} [리스트파일병합] ListFile병합완료 : {1:000} - {2} ", DateTime.Now, 파일cnt, new FileInfo(이동파일).Name);
                        로그기록(로그내용, 1);
                    }

                    상태.입력폴더 = 이동파일; // d:\Easy약관Plus\List\@완료\#KB_250726_103620\250726_103620_#KB_Lngtrm.dat.Re
                    로그내용 = string.Format("{0:yy.MM.dd HH:mm:ss} [리스트파일이동] ListFile이동완료 : 파일이동 - {1} ", DateTime.Now, 이동파일);
                    로그기록(로그내용, 1);
                    실행여부 = "1";
                    break;
                }
                catch (Exception ex)
                {
                    string 메세지 = ex.Message;
                    재시도++;
                    if (재시도 <= 3)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(5));
                        continue;
                    }
                    실행여부 = "실패";
                    throw;
                }
            }
            return 실행여부;
        }
        
        // ==========================================================================================================
        // 01 - 02 ListFile이 존재 하지 않으면 data처리 Table를 검색해서 기존 오류 또는 처리가 되지 않은 데이터를 처리한다.
        // ==========================================================================================================
        private async Task<string> data처리Table검색(현재상태 상태, string tmpgroupKey)
        {
            string 실행여부 = ""; 
            int 재시도 = 0;
            while (true)
            {
                try
                {    
                    using (SqlConnection AdoNet = new SqlConnection(상태.dataTable접속))
                    {
                        await AdoNet.OpenAsync();
                        DataSet ds = new DataSet();
                        DataTable dataTable = new DataTable();
                        string SqlStr = $@"
                                SELECT top(1) *
                                FROM data처리
                                WHERE groupKey = '{tmpgroupKey}' 
                                    AND 처리구분 = '{상태.처리구분}'
                                ORDER BY 시작시간
                            ";
                        using (SqlCommand sqlcmd = new SqlCommand(SqlStr, AdoNet))
                        using (SqlDataAdapter adapter = new SqlDataAdapter(sqlcmd))
                        {
                            adapter.Fill(ds, "기본");
                            if (ds.Tables["기본"].Rows.Count == 0)
                            {
                                실행여부 = "0"; //데이터 없음, 오류아님
                            } else {
                                DataRow row = ds.Tables["기본"].Rows[0];
                                DB2구조체Save(상태, row); //DB => 구조체 저장

                                string[] 분리 = row["처리단계"].ToString().Split('|').Select(x => x.Trim()).ToArray();
                                if (분리.Length >= 2)
                                {
                                    상태.처리Step = 분리[0];
                                    상태.처리여부 = 분리[1];

                                    if (상태.처리여부 == "완료")  //현재 처리Step이 완료 이면 다음단계로 이동한다.
                                    {
                                        int 다음단계 = Convert.ToInt16(상태.처리Step) + 1;
                                        상태.처리Step = string.Format("{0:00}", 다음단계);
                                    }
                                } else {
                                    상태.처리Step = "01"; // 값이 저장 되지 않은 상태로 처음부터 다시 시작한다.
                                }
                                실행여부 = "1"; //정상

                            }
                        }
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
                    throw;
                }
            }
            return 실행여부;
        }

        // ==========================================================================================================
        // 01 - 03 : ListFile to Table Upload : ListFile을 Table에 업로드한다.
        //           중복데이터는 제외한다. 
        //           TemgroupKey 생성 : 담보리스트파일명으로 생성 (250726_103620_#KB_Lngtrm.dat.Re)
        //           data처리에 제어를 필요한 데이터가 들어가 있다.
        // ==========================================================================================================
        private async Task<string> FileToTableUp(현재상태 상태)
        {
            string 실행여부 = "";
            로그내용 = string.Format("{0:yy.MM.dd HH:mm:ss} [데이터 업로드 시작] DATA를 TABLE에 UPLOAD 시작 : 파일명 - {1}", DateTime.Now, new FileInfo(상태.입력폴더).Name);
            로그기록(로그내용, 1);

            int 재시도 = 0;
            try
            {
                // 담보리스트 없거나 빈파일
                // d:\Easy약관Plus\List\@완료\#KB_250726_103620\250726_103620_#KB_Lngtrm.dat.Re
                if (!new FileInfo(상태.입력폴더).Exists || new FileInfo(상태.입력폴더).Length == 0)
                {
                    실행여부 = "0";
                    throw new Exception("[ERROR] dataUpload 오류 : ListFile 파일 미존재 또는 파일용량 0 byte");
                }
                else {

                    //  '.Re' or '.sub1 ~ sub3', '.main' 제외 ==> 210830_100925_#KB_F026_F_0826.lst 
                    string tmpkey = System.IO.Path.GetFileNameWithoutExtension(상태.입력폴더);
                    string 멀티확장자 = System.IO.Path.GetExtension(tmpkey).ToLower();
                    bool 멀티확장자확인 = 멀티확장자.StartsWith(".sub") || 멀티확장자.StartsWith(".main") ? true : false;

                    if (멀티확장자확인)
                        상태.groupKey = System.IO.Path.GetFileNameWithoutExtension(tmpkey);
                    else
                        상태.groupKey = tmpkey;
                }

                string SqlStr = string.Empty;
                while (true)
                {
                    //=============================================================================
                    // 증권+약관 출력인데 증권이 존재하지 않는것을 미리 제외하기 위해
                    //=============================================================================
                    string 증권입력폴더 = string.Format("{0}@증권\\{1}\\", 저장.기본입력폴더, 상태.고객사);
                    if (!Directory.Exists(증권입력폴더))
                    {
                        Directory.CreateDirectory(증권입력폴더);
                    }

                    List<string> 증권목록 = new List<string>();
                    foreach (System.IO.FileInfo 파일 in new System.IO.DirectoryInfo(증권입력폴더).GetFiles("*.pdf"))
                    {
                        증권목록.Add(System.IO.Path.GetFileNameWithoutExtension(파일.Name));
                    }

                    using (var AdoNet = new SqlConnection(상태.dataTable접속))
                    {
                        await AdoNet.OpenAsync();
                        using (var transaction = AdoNet.BeginTransaction())
                        {
                            try
                            {
                                List<string> Table저장오류 = new List<string>();

                                SqlStr = @"
                                    INSERT INTO data(
                                        처리구분, 고객사, groupKey, 고객Key, 상태, 파일명, 약관작업, PDF변환작업, PDF병합작업, 개별PDF작업, 우편작업, FTP작업, 모바일작업, 전체약관경로, data
                                    ) VALUES(
                                       @처리구분, @고객사, @groupKey, @고객Key, @상태, @파일명, @약관작업, @PDF변환작업, @PDF병합작업, @개별PDF작업, @우편작업, @FTP작업, @모바일작업, @전체약관경로, @data
                                    )";
                                //ListFile 읽기
                                string[] data = System.IO.File.ReadAllLines(상태.입력폴더, Encoding.GetEncoding(949));

                                string 파일명만 = System.IO.Path.GetFileNameWithoutExtension(상태.입력폴더);
                                int 인수 = 상태.입력폴더.IndexOf("[M") + 2; // 파명일: [M05]~.sub1

                                
                                if (!int.TryParse(상태.입력폴더.Substring(인수, 2), out int 병렬cnt))
                                    병렬cnt = 1;

                                if (chk싱글처리.Checked)
                                    병렬cnt = 1;

                                List<string> PK중복 = new List<string>();
                                int 입력cnt = 0;
                                for (int i = 0; i < data.Length; i++)
                                {
                                    if (data[i].Trim().Length == 0){
                                        continue;
                                    }

                                    string[] 분리 = data[i].Split('|').Select(x => x.Trim()).ToArray();
                                    string 고개번호 = 분리[0];
                                    // PK값 중복발생
                                    if (PK중복.IndexOf(고개번호) != -1)
                                    {
                                        // 아래 시작하는 화면에서 출력 
                                        로그내용 = string.Format("{0:yy.MM.dd HH:mm:ss} [ERROR] 중복고객번호(PK)발생 : 고객번호 - {1}, 라인 - {2}", DateTime.Now, 고개번호, i + 1);
                                        Table저장오류.Add(로그내용);
                                        continue;
                                    }
                                    else {
                                        PK중복.Add(고개번호);
                                    }
                                    상태.고객Key = 고개번호; // PK값으로 사용할 고객번호

                                    // 증권+약관 인데 증권이 없으면 제외
                                    string tmp서식구분 = 분리[10].Trim();
                                    if (tmp서식구분 == "01" || tmp서식구분 == "1")
                                    {
                                        if (증권목록.IndexOf(상태.고객Key) == -1)
                                        {
                                            로그내용 = string.Format("{0:yy.MM.dd HH:mm:ss} [ERROR] 서식구분[{3}](증권+약관)형식, 증권미존재 : 고객번호 - {1}, 라인 - {2}", DateTime.Now, 상태.고객Key, i + 1, tmp서식구분);
                                            Table저장오류.Add(로그내용);
                                            continue;
                                        }
                                    }

                                    // *** KB손해보험만 해당 ***
                                    // 들어오는 List파일을 읽어 상품구매일을 개정일로 바꾼다.
                                    // 상품구매일 근사값의 개정일을 구해야 한다. ( 개정일은 상품구매일보다 작은 가장 가까운 개정일)
                                    // H,T PDF생성 때문에 미리 개정일을 이곳에서 생성한다.
                                    // 분리[2] 개정일
                                    // 분리[1] 상품코드

                                    string 전체약관경로 = "";
                                    string tmp약관작업 = "대기";

                                    // ======================================
                                    // KB손해보험 별도 로직(통약관, 개정일)
                                    // ======================================
                                    if (상태.고객사 == "KB" )
                                    {
                                        string tmp테이블 = "";
                                        string tmp상품코드 = 분리[1];
                                        string tmp개정일 = (분리[2].Length == 6) ? 분리[2] : 분리[2] + "30";  //6:년(2)월(2)일(2), 4:년(2)월(2)

                                        //3.33만 가능(KB손보 장기모바일 시스템)
                                        bool 전체약관가능여부 = false;
                                        if (저장.사용컴퓨터 != "dps77") //종우컴 제외
                                        {
                                            전체약관가능여부 = NetworkManager.고객사선택(상태.고객사, 저장.사용컴퓨터) == "KB2" ? true : false;
                                        }
                                        bool 모바일만가능 = (분리[11] == "02" || 분리[11] == "2") ? true : false;


                                        //전체약관인지 여부 확인
                                        if (전체약관가능여부  && 모바일만가능 && 분리.Length > 18 && Array.IndexOf(분리, "*", 18) >= 0)
                                        {
                                            string tmp종분류코드 = string.IsNullOrWhiteSpace(분리[9]) ? "0" : 분리[9].Trim();
                                            전체약관경로 = 전체약관경로찾기(tmp상품코드, tmp종분류코드, Convert.ToInt32(tmp개정일));
                                        }

                                        int 현재년도 = Convert.ToInt32( string.Format("{0:yy}", DateTime.Now));
                                        int 개정년도 = Convert.ToInt32(tmp개정일.Substring(0, 2));

                                        // 개정년도가 현재년도 보다 작으면 예전 Tabel을 가져온다 ex)담보KB_22
                                        // 아니면 기본테이블                                     ex)담보KB
                                        int 마지막년도 = 20;
                                        for (int y = 개정년도; y >= 마지막년도; y--)
                                        {
                                            string 테이블년도 = "";
                                            if (현재년도 != y)
                                                테이블년도 = $"_{y}";

                                            tmp테이블 = $"담보KB{테이블년도}";
                                            string 찾기tmp개정일 = KB손보개정일찾기(tmp테이블, tmp상품코드, Convert.ToInt32(tmp개정일));

                                            if (!string.IsNullOrEmpty(찾기tmp개정일))
                                            {
                                                분리[2] = 찾기tmp개정일;
                                                data[i] = string.Join("|", 분리);  // Array to String 변환
                                                break;
                                            }
                                            
                                            if (y == 마지막년도)
                                            {
                                                //KB는 개별처리를 해야한다 묶음처리로 이루어 졌지만 오류가 아닌건 처리하는쪽으로 (장기 모바일 때문)
                                                tmp약관작업 = "약관오류";
                                                //throw new Exception($"[ERROR] KB손해보험 개정일 오류/실패 : 고객번호 - {상태.고객Key}");
                                            }
                                        }
                                    }
                                    string 처리일자 = string.Format("{0:MMdd}", DateTime.Now);
                                    string 상품코드 = 분리[1];
                                    string 개정일 = 분리[2];
                                    string 고객번호 = 분리[0];
                                    string 파일명 = string.Format("{0}_{1}{2}_{3}", 처리일자, 상품코드, 개정일, 고객번호);

                                    using (SqlCommand sqlcmd = new SqlCommand(SqlStr, AdoNet, transaction))
                                    {
                                        sqlcmd.Parameters.AddWithValue("@처리구분", 상태.처리구분);
                                        sqlcmd.Parameters.AddWithValue("@고객사", 상태.고객사);
                                        sqlcmd.Parameters.AddWithValue("@groupKey", 상태.groupKey);
                                        sqlcmd.Parameters.AddWithValue("@고객Key", 상태.고객Key);
                                        sqlcmd.Parameters.AddWithValue("@상태", "대기");   
                                        sqlcmd.Parameters.AddWithValue("@파일명", 파일명);
                                        sqlcmd.Parameters.AddWithValue("@약관작업", tmp약관작업);
                                        sqlcmd.Parameters.AddWithValue("@PDF변환작업", "대기");
                                        sqlcmd.Parameters.AddWithValue("@PDF병합작업", "대기");
                                        sqlcmd.Parameters.AddWithValue("@개별PDF작업", "대기");
                                        sqlcmd.Parameters.AddWithValue("@우편작업", "대기");
                                        sqlcmd.Parameters.AddWithValue("@FTP작업", "대기");
                                        sqlcmd.Parameters.AddWithValue("@모바일작업", "대기");
                                        sqlcmd.Parameters.AddWithValue("@전체약관경로", 전체약관경로);
                                        sqlcmd.Parameters.AddWithValue("@data", data[i]);

                                        int result = await sqlcmd.ExecuteNonQueryAsync();
                                        if(result > 0)
                                        {
                                            입력cnt++;
                                        } else
                                        {
                                            로그내용 = string.Format("{0:yy.MM.dd HH:mm:ss} [ERROR] 데이터입력 오류 : 고객번호 - {1}, 라인 : {2}, 상품코드 - {3}", DateTime.Now, 상태.고객Key, i + 1, 분리[1]);
                                            Table저장오류.Add(로그내용);
                                        }
                                    }
                                }

                                상태.출력폴더 = 저장.기본출력폴더;
                                if (저장.사용컴퓨터.Contains("main") || 저장.사용컴퓨터.Contains("sub") || 상태.고객사 == "KB" || 상태.고객사 == "DB" || 상태.고객사 == "DZ")
                                {
                                    // ex) 출력폴더\\고객사(KB)\년월(yyyy.MM)\일(dd)\시분초(HHmmss) ==> ex) d:\Easy약관Plus\Output\KB\2021.02\19\182855\XXXXX.pdf
                                    상태.출력폴더 = string.Format(@"{0}{1}\{2:yyyy.MM|dd|HHmmss|}", 저장.기본출력폴더, 상태.고객사, DateTime.Now);
                                    상태.출력폴더 = 상태.출력폴더.Replace('|', '\\');
                                }
                                else
                                {
                                    상태.출력폴더 = string.Format(@"{0}{1}\", 저장.기본출력폴더, 상태.고객사);
                                }

                                for (int i = 0; i < Table저장오류.Count; i++)
                                {
                                    로그내용 = Table저장오류[i];
                                    로그기록(로그내용, 1);
                                }

                                //입력데이터가 모두가 오류이면 저장('data처리')을 제외한다.
                                if (입력cnt == 0)
                                {
                                    실행여부 = "0";
                                    throw new Exception("[ERROR] 데이터입력 오류 : 모든데이터 오류로 인해 업로드를 하지 못 했습니다. 로그를 참조");
                                }


                                string 처리Sql = $@"
                                    INSERT INTO data처리 (
                                        groupKey, 처리구분, 고객사, 상태, 시작시간, 완료시간, 전체건수, 병렬컴, 처리단위, 
                                        처리단계, 실행횟수, 약관작업, PDF변환작업, PDF병합작업, 개별PDF작업, 우편작업, FTP작업, 모바일작업, 앱실행수, 출력폴더, 입력폴더
                                    ) VALUES (
                                        @groupKey, @처리구분, @고객사, @상태, @시작시간, @완료시간, @전체건수, @병렬컴, @처리단위, 
                                        @처리단계, @실행횟수, @약관작업, @PDF변환작업, @PDF병합작업, @개별PDF작업, @우편작업, @FTP작업, @모바일작업, @앱실행수, @출력폴더, @입력폴더
                                    )";

                                
                                using (SqlCommand sqlcmd2 = new SqlCommand(처리Sql, AdoNet, transaction))
                                {
                                    sqlcmd2.Parameters.AddWithValue("@groupKey", 상태.groupKey);
                                    sqlcmd2.Parameters.AddWithValue("@처리구분", 상태.처리구분);
                                    sqlcmd2.Parameters.AddWithValue("@고객사", 상태.고객사);
                                    sqlcmd2.Parameters.AddWithValue("@상태", "처리");
                                    sqlcmd2.Parameters.AddWithValue("@시작시간", DateTime.Now.ToString("yy.MM.dd (HH:mm:ss) FFFF"));
                                    sqlcmd2.Parameters.AddWithValue("@완료시간", "");
                                    sqlcmd2.Parameters.AddWithValue("@전체건수", 입력cnt.ToString());
                                    sqlcmd2.Parameters.AddWithValue("@병렬컴", 병렬cnt);
                                    sqlcmd2.Parameters.AddWithValue("@처리단위", 상태.상태컴퓨터);
                                    sqlcmd2.Parameters.AddWithValue("@처리단계", "01|완료");
                                    sqlcmd2.Parameters.AddWithValue("@실행횟수", 0);
                                    sqlcmd2.Parameters.AddWithValue("@약관작업", "대기");
                                    sqlcmd2.Parameters.AddWithValue("@PDF변환작업", "대기");
                                    sqlcmd2.Parameters.AddWithValue("@PDF병합작업", "대기");
                                    sqlcmd2.Parameters.AddWithValue("@개별PDF작업", "대기");
                                    sqlcmd2.Parameters.AddWithValue("@우편작업", "대기");
                                    sqlcmd2.Parameters.AddWithValue("@FTP작업", "대기");
                                    sqlcmd2.Parameters.AddWithValue("@모바일작업", "대기");;
                                    sqlcmd2.Parameters.AddWithValue("@앱실행수", Convert.ToInt32(cob스레드수.EditValue));
                                    sqlcmd2.Parameters.AddWithValue("@출력폴더", 상태.출력폴더);
                                    sqlcmd2.Parameters.AddWithValue("@입력폴더", 상태.입력폴더);

                                    int result = await sqlcmd2.ExecuteNonQueryAsync(); // 1:성공, 0:오류는 없지만 실패, -1:오류발생 실패
                                    if (result <= 0) // data처리 Table는 반드시 입력 되어야 한다.
                                    {
                                        throw new Exception("[ERROR] 데이터입력 오류 : data처리 Table에 데이터가 입력 되지 못 했습니다.");
                                    }
                                }
                              
                                transaction.Commit();
                                상태.처리Step = "02";
                                실행여부 = "1";
                                
                                로그내용 = string.Format("{0:yy.MM.dd HH:mm:ss} [데이터 업로드 완료] === data를 Table에 UPLOAD를 완료합니다. UPLOAD Row Count - {1:00000}  ===", DateTime.Now, data.Length);
                                로그기록(로그내용, 1);

                                string 작업완료파일 = $"{저장.기본출력폴더}감시\\{상태.groupKey}.단계00";
                                File.WriteAllText(작업완료파일, "data를 Table에 업로드 완료", Encoding.GetEncoding(949));

                                break;
                            }
                            catch (Exception ex)
                            {
                                string 오류 = ex.Message;
                                transaction.Rollback();
                                재시도++;
                                if (재시도 <= 3)  {
                                    await Task.Delay(TimeSpan.FromSeconds(5));
                                    continue;
                                }
                                실행여부 = "실패";
                                throw;
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
            return 실행여부;
        }
        /// <summary>
        /// KB손해보험 (상품구매일 ==> 개정일로 변경)
        /// </summary>
        /// 
        // ==========================================================================================================
        // KB손해보험 계약일 ==> 개정일 
        // 담보KB 테이블을 읽어 캐쉬에 저장한 후 변경사항(감시)이 발생하면 다시 'KB손보개정일저장'을 실행한다.
        // DB에서 Service Broker 켜져 있는지 확인
        // SELECT is_broker_enabled FROM sys.databases WHERE name = '약관' (0:비활성화, 1:활성화)
        // ALTER DATABASE 약관 SET ENABLE_BROKER WITH ROLLBACK IMMEDIATE;
        // ==========================================================================================================
        private async Task KB손보테이블_감시()
        {
            try
            {
                using (SqlConnection AdoNet = new SqlConnection(저장.약관DB))
                {
                    await AdoNet.OpenAsync();
                    string sql = "SELECT 상품코드, 개정일 FROM dbo.담보KB ";
                    using (SqlCommand cmd = new SqlCommand(sql, AdoNet))
                    {
                        var dep = new SqlDependency(cmd);
                        dep.OnChange += Dep_OnChange; // 명시적 이벤트 등록
                        using (SqlDataReader reader = await cmd.ExecuteReaderAsync()) { while (await reader.ReadAsync()) { } }
                    }
                }
            }
            catch (Exception ex)
            {
                로그기록($"[오류] 담보KB 감시 설정 실패: {ex.Message}", 1);
                await Task.Delay(5000);
                _ = KB손보테이블_감시();
            }
        }

        private async void Dep_OnChange(object sender, SqlNotificationEventArgs e)
        {
            if (sender is SqlDependency dep)
            {
                dep.OnChange -= Dep_OnChange; // 즉시 이벤트 해제
            }

            lock (_담보KBLock)
            {
                _담보KBCts?.Cancel();
                _담보KBCts = new CancellationTokenSource();
            }
            CancellationToken token = _담보KBCts.Token;

            try
            {
                await Task.Delay(3000, token); // 3초 대기 (이벤트 폭풍 디바운스)

                if (e.Type == SqlNotificationType.Change &&
                    (e.Info == SqlNotificationInfo.Insert || e.Info == SqlNotificationInfo.Update || e.Info == SqlNotificationInfo.Delete))
                {
                    로그내용 = string.Format("{0:yy.MM.dd HH:mm:ss} [KB손보] 담보KB 대량 변경 감지. 메모리 갱신...", DateTime.Now);
                    로그기록(로그내용, 1);
                    await KB손보개정일저장(1);
                }
            }
            catch (TaskCanceledException) { return; }
            catch (Exception ex)
            {
                로그기록($"[오류] 담보KB 갱신 중 예외: {ex.Message}", 1);
            }
            finally
            {
                if (!token.IsCancellationRequested)
                {
                    _ = KB손보테이블_감시(); // 최종적으로 다시 감시망 가동
                }
            }
        }

        private async Task<string> KB손보개정일저장(int 옵션) // 옵션:0 최초 처음, 모든테이블 읽기, 1: 변경이벤트 발생시 담보KB만 읽기
        {
            int 재시도 = 0;
            string 실행여부 = "성공";

            while (true)
            {
                KB개정일 = new ConcurrentDictionary<string, Dictionary<string, List<int>>>();
                int 테이블인수 = 1; 
                if (옵션 == 0)
                {
                    KB개정일이전 = new ConcurrentDictionary<string, Dictionary<string, List<int>>>();  // 최초 처음만
                    테이블인수 = DateTime.Now.Year - 2020 + 1;
                }

                int 현재연도 = Convert.ToInt32(string.Format("{0:yy}", DateTime.Now));

                string[] 테이블들 = Enumerable.Range(0, 테이블인수)
                            .Select(i => i == 0 ? "담보KB" : $"담보KB_{현재연도 - i}")
                            .ToArray();
                
                try
                {
                    using (SqlConnection Adonet = new SqlConnection(저장.약관DB))
                    {
                        await Adonet.OpenAsync();

                        foreach (var 테이블 in 테이블들)
                        {
                            var dict = new Dictionary<string, List<int>>();

                            // CROSS APPLY와 STRING_SPLIT을 사용하여 쉼표로 연결된 코드를 행(Row)으로 분리하여 가져옵니다.
                            string sql = $"SELECT value AS 상품코드, 개정일 FROM {테이블} CROSS APPLY STRING_SPLIT(상품코드, ',') GROUP BY value, 개정일";

                            using (SqlCommand cmd = new SqlCommand(sql, Adonet))
                            using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                            {
                                while (await reader.ReadAsync())
                                {
                                    string code = reader["상품코드"].ToString();
                                    int 개정일 = Convert.ToInt32(reader["개정일"]);

                                    if (!dict.TryGetValue(code, out var list))
                                    {
                                        list = new List<int>();
                                        dict[code] = list;
                                    }
                                    list.Add(개정일);
                                }
                            }

                            // 내림차순 정렬 (계약일 검색 시 빠르게 찾도록)
                            foreach (var key in dict.Keys.ToList())
                                dict[key].Sort((a, b) => b.CompareTo(a));

                            if (테이블 == "담보KB") // Dictionary를 2개로 관리해서 이벤트가 발생하면 담보KB만 다시 저장한다.
                                KB개정일[테이블] = dict;
                            else
                                KB개정일이전[테이블] = dict;
                        }
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
                    throw;
                }
            }
            return 실행여부;
        }

        // =========================================================================
        // 🟢 [신규 추가] 약관_KB 메모리 로드 (변경 발생 시 다시 읽음)
        // =========================================================================
        private async Task 약관KB_메모리로드()
        {
            var 임시캐시 = new Dictionary<string, List<약관정보>>();

            // 파일경로가 존재하는 유효한 데이터만 조회
            // STRING_SPLIT을 적용하여 메모리에 개별 상품코드 키가 생성되도록 합니다.
            string sql = "SELECT value AS 상품코드, 담보분류, 개정일, 파일경로 FROM dbo.약관_KB CROSS APPLY STRING_SPLIT(상품코드, ',') WHERE 파일경로 IS NOT NULL";

            using (SqlConnection Adonet = new SqlConnection(저장.약관DB))
            {
                await Adonet.OpenAsync();
                using (SqlCommand cmd = new SqlCommand(sql, Adonet))
                using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        string 상품코드 = reader["상품코드"].ToString();
                        string 담보분류 = reader["담보분류"].ToString();
                        int 개정일 = Convert.ToInt32(reader["개정일"]);
                        string 파일경로 = reader["파일경로"].ToString();

                        string key = $"{상품코드}|{담보분류}"; // 복합 키 생성

                        if (!임시캐시.ContainsKey(key))
                        {
                            임시캐시[key] = new List<약관정보>();
                        }
                        임시캐시[key].Add(new 약관정보 { 개정일 = 개정일, 파일경로 = 파일경로 });
                    }
                }
            }

            // 개정일 기준 내림차순(최신순) 정렬 -> 나중에 검색할 때 첫 번째 매칭값이 정답이 되도록 세팅
            foreach (var key in 임시캐시.Keys.ToList())
            {
                임시캐시[key] = 임시캐시[key].OrderByDescending(x => x.개정일).ToList();
            }

            // 스레드 안전성을 위해 다 만들어진 딕셔너리를 한 번에 교체
            전체약관캐시 = new ConcurrentDictionary<string, List<약관정보>>(임시캐시);
        }

        // =========================================================================
        // 🟢 [신규 추가] 약관_KB 테이블 감시 (기존 로직과 동일한 패턴)
        // =========================================================================
        private async Task 약관KB테이블_감시()
        {
            try
            {
                using (SqlConnection AdoNet = new SqlConnection(저장.약관DB))
                {
                    await AdoNet.OpenAsync();
                    string sql = "SELECT 상품코드, 개정일, 담보분류, 작성일, 파일경로 FROM dbo.약관_KB";
                    using (SqlCommand cmd = new SqlCommand(sql, AdoNet))
                    {
                        var dep = new SqlDependency(cmd);
                        dep.OnChange += 약관KB_OnChange; // 명시적 이벤트 등록
                        using (SqlDataReader reader = await cmd.ExecuteReaderAsync()) { while (await reader.ReadAsync()) { } }
                    }
                }
            }
            catch (Exception ex)
            {
                로그기록($"[오류] 약관_KB 감시 설정 실패: {ex.Message}", 1);
                await Task.Delay(5000);
                _ = 약관KB테이블_감시();
            }
        }

        private async void 약관KB_OnChange(object sender, SqlNotificationEventArgs e)
        {
            if (sender is SqlDependency dep)
            {
                dep.OnChange -= 약관KB_OnChange; // 즉시 이벤트 해제
            }

            lock (_약관KBLock)
            {
                _약관KBCts?.Cancel();
                _약관KBCts = new CancellationTokenSource();
            }
            CancellationToken token = _약관KBCts.Token;

            try
            {
                await Task.Delay(3000, token); // 3초 대기 (이벤트 폭풍 디바운스)

                if (e.Type == SqlNotificationType.Change)
                {
                    로그내용 = string.Format("{0:yy.MM.dd HH:mm:ss} [KB손보] 약관_KB 대량 변경 감지. 메모리(통약관) 갱신...", DateTime.Now);
                    로그기록(로그내용, 1);
                    await 약관KB_메모리로드();
                }
            }
            catch (TaskCanceledException) { return; }
            catch (Exception ex)
            {
                로그기록($"[오류] 약관_KB 갱신 중 예외: {ex.Message}", 1);
            }
            finally
            {
                if (!token.IsCancellationRequested)
                {
                    _ = 약관KB테이블_감시(); // 최종적으로 다시 감시망 가동
                }
            }
        }

        private string 전체약관경로찾기(string 상품코드, string 담보분류, int 기준개정일)
        {
            string key = $"{상품코드}|{담보분류}";

            if (전체약관캐시.TryGetValue(key, out var 약관목록))
            {
                // 내림차순 정렬되어 있으므로, 기준개정일보다 작거나 같은(이전인) 것 중 첫 번째가 가장 최신 과거 개정일
                var 매칭약관 = 약관목록.FirstOrDefault(x => x.개정일 <= 기준개정일);

                if (매칭약관 != null)
                {
                    return 매칭약관.파일경로;
                }
            }
            return ""; // 못 찾았을 경우 빈 문자열
        }

        private string KB손보개정일찾기(string 테이블명, string 상품코드, int 계약일)
        {
            List<int> 개정일리스트 = null;
            if (KB개정일.TryGetValue(테이블명, out var dict1) &&
                    dict1.TryGetValue(상품코드, out 개정일리스트))
            {
                // found
            }
            // 못 찾았으면 이전 딕셔너리에서 찾기
            else if (KB개정일이전.TryGetValue(테이블명, out var dict2) &&
                     dict2.TryGetValue(상품코드, out 개정일리스트))
            {
                // found
            }

            if (개정일리스트 == null || 개정일리스트.Count == 0)
                return null;

            // 리스트는 내림차순 정렬되어 있으므로 계약일 이하 첫 번째 값 리턴
            foreach (var d in 개정일리스트)
            {
                if (d <= 계약일)
                    return d.ToString();
            }

            return null; // 못 찾음
        }
        
        // ==========================================================================================================
        // 02 - 01 출력에 필요한 폴더를 생성한다.
        //         상품별 샘플출력은 상품명을 '*'치환한다.
        //         출력폴더 : 기본출력폴더에서 고객사,날짜,시간,초 등으로 변경됨 (주의요망) - 따라서 파일이 출력되는 위치
        //         기본출력폴더 : 기본위치 (d:\Easy약관Plus\Output\)
        //         ListFile의 내용을 뷰에 표시한다.
        //         구조체에 저장
        // ==========================================================================================================
        private async Task<string> Data2구조체(현재상태 상태, string 개별)
        {
            int 재시도 = 0;
            string 실행여부 = "성공";

            while (true)
            {
                try
                {
                    using (SqlConnection AdoNet = new SqlConnection(상태.dataTable접속))
                    {
                        await AdoNet.OpenAsync();

                        DataSet ds = new DataSet();
                        DataTable dataTable = new DataTable();
                        //처리구분 : 00, 01, 03....(병렬처리)

                        if (개별 == "개별")
                        {
                            string SqlStr = $@"
                                    SELECT top(1) *
                                    FROM data처리
                                    WHERE 상태 = '처리'
                                        AND 처리구분 = '{상태.처리구분}'
                                        AND groupKey = '{상태.groupKey}'
                                    ORDER BY 시작시간
                            ";
                            SqlCommand sqlcmd = new SqlCommand(SqlStr, AdoNet);
                            SqlDataAdapter adapter = new SqlDataAdapter(sqlcmd);
                            adapter.Fill(ds, "기본");

                            DataColumn 확인01 = new DataColumn("확인", typeof(bool)); // 확인 필드추가
                            확인01.DefaultValue = false;
                            ds.Tables["기본"].Columns.Add(확인01);
                            확인01.SetOrdinal(0);

                            DataRow row = ds.Tables["기본"].Rows[0];
                            DB2구조체Save(상태, row);

                            List<string> 숨김필드 = new List<string>() { 
                            "완료시간","출력폴더","입력폴더"
                            };
                        
                            foreach(string 필드 in 숨김필드)
                                ds.Tables["기본"].Columns[필드].ColumnMapping = MappingType.Hidden;

                            // ===========================================
                            //출력관련 폴더생성
                            // ===========================================
                            if (await 실행(() => 폴더생성상세(상태), "폴더생성상세") == "실패")
                            {
                                throw new Exception("ER00_🚫진행불가 : 폴더생성상세 오류, 로그참조");
                            }
                        
                            ds.Tables["기본"].Columns["출력폴더"].ColumnMapping = MappingType.Hidden;  // 컬럼 숨김
                        
                            SqlStr = string.Format("SELECT * FROM data WHERE 고객사 = '{0}' and 처리구분 = '{1}' and groupKey = '{2}' Order by 고객Key", 상태.고객사, 상태.처리구분 , 상태.groupKey);   //처리구분 = '{0}' and 
                            sqlcmd = new SqlCommand(SqlStr, AdoNet);
                            adapter = new SqlDataAdapter(sqlcmd);
                            adapter.Fill(ds, "상세");

                            DataColumn 확인02 = new DataColumn("확인", typeof(bool)); // 확인 필드추가
                            확인02.DefaultValue = false;
                            ds.Tables["상세"].Columns.Add(확인02);
                            확인02.SetOrdinal(0);
                            ds.Tables["상세"].Columns["groupKey"].ColumnMapping = MappingType.Hidden;  // 컬럼 숨김
                            ds.Tables["상세"].Columns["파일명"].ColumnMapping = MappingType.Hidden;
                            ds.Tables["상세"].Columns["data"].ColumnMapping = MappingType.Hidden;

                            DataColumn KeyColumn = ds.Tables["기본"].Columns["groupKey"];
                            DataColumn ForeignKeyColumn = ds.Tables["상세"].Columns["groupKey"];

                            ds.Relations.Add("상세", KeyColumn, ForeignKeyColumn);

                            grd작업리스트.DataSource = ds.Tables[0];
                            grd작업리스트.ForceInitialize();

                            GridView GDOPView = new GridView(grd작업리스트);
                            grd작업리스트.LevelTree.Nodes.Add("상세", GDOPView);
                            GDOPView.ViewCaption = "레코드 상세 리스트";
                            GDOPView.GroupPanelText = "처리 할 내용을 보여 줍니다.";
                            view작업리스트.ExpandMasterRow(0); //0:해당라인, 펼치기

                            view작업리스트.Appearance.Row.Font = new Font("카이겐고딕 KR Regular", 9);
                            if (await 실행(() => grdListFile_View(1), "grdListFile_View") == "실패")
                            {
                                throw new Exception("ER00_🚫진행불가 : grdListFile_View 오류, grdListFile_View");
                            }

                        } else if (개별 == "전체")
                        {
                            string SqlStr = $@"
                                    SELECT *
                                    FROM  data
                                    WHERE groupKey = '{상태.groupKey}'
                            ";
                            SqlCommand sqlcmd = new SqlCommand(SqlStr, AdoNet);
                            SqlDataAdapter adapter = new SqlDataAdapter(sqlcmd);
                            adapter.Fill(ds, "상세");

                        }

                        // ===========================================
                        // 레코드 구조체 저장 
                        // ===========================================
                        if (await 실행(() => data저장(ds, 상태), "data저장") == "실패")
                        {
                            throw new Exception("ER00_🚫진행불가 : 구조체 저장 오류, data저장");
                        }
                        break;
                    }
                }
                catch (Exception e)
                {
                    if (e.Message .Substring(0,4) != "ER00")
                    {
                        재시도++;
                        if (재시도 <= 3)
                        {
                            await Task.Delay(TimeSpan.FromSeconds(5));
                            continue;
                        }
                    }
                    실행여부 = "실패";
                    throw;
                }
            }
            return 실행여부;
        }

        // ==========================================================================================================
        // 02 - 02 : 출력에 필요한 DB내용을 레코드 구조체에 저장한다. 수정시 : 04 - 선택약관PDF병합 : 세션별로 되어 있는 PDF파일을 하나로 병합 하는 단계(함께수정)
        // ==========================================================================================================
        private async Task<string> data저장(DataSet ds, 현재상태 상태)
        {
            string 실행여부 = "성공";
            
            try
            {
                레코드s = new List<레코드>();
                for (int i = 0; i < ds.Tables["상세"].Rows.Count; i++)
                {
                    string[] 분리 = ds.Tables["상세"].Rows[i]["data"].ToString().Split('|').Select(P => P.Trim()).ToArray();

                    string str담보 = "";
                    int 담보시작위치 = 18;
                    int 담보종료위치 = 분리.Length;
                    string[] tmp담보 = new string[담보종료위치 - 담보시작위치];
                    Array.Copy(분리, 담보시작위치, tmp담보, 0, 담보종료위치 - 담보시작위치);
                    str담보 = string.Join("|", tmp담보); // Array to string 변환
                    
                    string 파일명 = string.Format("{0}", ds.Tables["상세"].Rows[i]["파일명"].ToString());

                    string 임시종분류 = 분리[9];
                    string 종분류코드 = "";
                    if (string.IsNullOrWhiteSpace(임시종분류))
                    {
                        종분류코드 = "";
                    }
                    else
                    {
                        종분류코드 = (상태.고객사 == "DB") ? string.Format("{0:00}", Convert.ToInt32(임시종분류)) : 임시종분류;   // DB는 종분류코드값이 1자리로 들어온다.
                    }

                    string 담보코드 = str담보;
                    if (담보코드.Length != 0 && 담보코드.Substring(담보코드.Length - 1) == "|")
                    {
                        // 담보맨뒤 '|'로 끝나면 삭제한다.  ex) 1234|4566|7458| ==> 1234|4566|7458
                        담보코드 = 담보코드.Substring(0, 담보코드.Length - 1);
                    }

                    레코드s.Add(new 레코드()
                    {
                        고객번호 = 분리[0],
                        상품코드 = 분리[1],
                        개정일 = 분리[2],
                        상품명 = 분리[3],
                        고객명 = 분리[4],
                        전화번호 = 분리[5],
                        이메일 = 분리[6],
                        우편번호 = 분리[7],
                        주소 = 분리[8],
                        종분류코드 = 종분류코드,
                        서식구분 = 분리[10],
                        발송구분 = 분리[11],
                        Password = 분리[12],
                        tmp02 = 분리[13],
                        tmp03 = 분리[14],
                        tmp04 = 분리[15],
                        tmp05 = 분리[16],
                        담보갯수 = 분리[17],
                        담보코드 = 담보코드,

                        파일명 = 파일명,
                        출력폴더 = 상태.출력폴더,
                        처리구분 = ds.Tables["상세"].Rows[i]["처리구분"].ToString(),
                        고객사 = ds.Tables["상세"].Rows[i]["고객사"].ToString(),
                        groupKey = 상태.groupKey,
                        상태 = 상태.상태,

                        약관작업 = ds.Tables["상세"].Rows[i]["약관작업"].ToString(),
                        PDF변환작업 = ds.Tables["상세"].Rows[i]["PDF변환작업"].ToString(),
                        PDF병합작업 = ds.Tables["상세"].Rows[i]["PDF병합작업"].ToString(),
                        개별PDF작업 = ds.Tables["상세"].Rows[i]["개별PDF작업"].ToString(),
                        우편작업 = ds.Tables["상세"].Rows[i]["우편작업"].ToString(),
                        FTP작업 = ds.Tables["상세"].Rows[i]["FTP작업"].ToString(),
                        모바일작업 = ds.Tables["상세"].Rows[i]["모바일작업"].ToString(),
                        전체약관경로 = ds.Tables["상세"].Rows[i]["전체약관경로"].ToString()
                    }); 
                }
                로그내용 = string.Format("{0:yy.MM.dd HH:mm:ss} [구조체 저장] data to struct 저장 완료: ListFile to 레코드 저장 ", DateTime.Now);
                로그기록(로그내용, 1);
            }
            catch (Exception)
            {
                await Task.Delay(TimeSpan.FromSeconds(0.01));
                실행여부 = "실패";
                throw;
            }
            return 실행여부;
            

        }

        // ==========================================================================================================
        // 02 - 03 : DB to 구조체 Save
        // ==========================================================================================================
        private void DB2구조체Save<T>(T target, DataRow row)
        {
            var props = typeof(T).GetProperties();
            foreach (var prop in props)
            {
                if (row.Table.Columns.Contains(prop.Name) && prop.CanWrite)
                {
                    var value = row[prop.Name];
                    if (value != DBNull.Value)
                    {
                        prop.SetValue(target, Convert.ChangeType(value, prop.PropertyType));
                    }
                }
            }
        }

        // ==========================================================================================================
        // 03 - 01 : 기본_H,T,S,I, 목차 파일 저장 
        //           상품별 해당 파일을 미리저장하여 DB 과접속을 사전에 줄일려는 목적임
        // ==========================================================================================================
        private async Task<string> 약관생성(현재상태 상태)
        {
            string 실행여부 = "성공";

            로그내용 = string.Format("{0:yy.MM.dd HH:mm:ss} [약관생성]입력폴더 : {1}", DateTime.Now, 저장.기본입력폴더);
            로그기록(로그내용, 1);
            로그내용 = string.Format("{0:yy.MM.dd HH:mm:ss} [약관생성]출력폴더 : {1}", DateTime.Now, 저장.기본출력폴더);
            로그기록(로그내용, 1);
            로그내용 = string.Format("{0:yy.MM.dd HH:mm:ss} [약관생성]작업파일 : {1}", DateTime.Now, 상태.입력폴더);
            로그기록(로그내용, 1);

            
            
            try
            {
                // 1. 조건에 맞는 전체 대상(약관작업 완료 안 된 건)을 먼저 추출합니다.
                var 전체진행대상 = (from tmp in 레코드s
                              where tmp.약관작업 != "약관완료"
                                 && tmp.처리구분 == $"{상태.처리구분}"
                              select tmp).ToList();

                // 2. 전체 대상 중 약관 생성이 필요 없는(03, 3) 자동 완료 대상을 분리합니다.
                var 자동완료대상 = 전체진행대상.Where(x => x.서식구분 == "03" || x.서식구분 == "3").ToList();

                // 3. 실제로 HWP를 통해 약관을 생성해야 할 대상(01, 1 등)만 남깁니다.
                var 생성대상목록 = 전체진행대상.Where(x => x.서식구분 != "03" && x.서식구분 != "3").ToList();



                저장.상품별세션 = new Dictionary<string, string>();
                foreach (var 레코드 in 생성대상목록) // <-- 생성대상목록만 순회
                {
                    if (!string.IsNullOrEmpty(레코드.전체약관경로))
                        continue;

                    if (await 실행(() => 기본약관PDF생성(상태, 레코드), "기본약관PDF생성") == "실패")
                    {
                        _ = await 실행(() => 진행불가DB업데이트(상태), "진행불가DB업데이트");
                        throw new Exception("ER00_??진행불가 : 기본약관PDF생성 오류, 로그참조");
                    }
                }

                // F1 ~ F8앱 시작을 위한 groupKey 파일생성
                // *.groupKey파일이 생성되면 F1 ~ F8앱이 감시 후 시작한다. 
                // Fn앱이 시작되면 *.start 파일생성, 종료되면 *.end 생성

                string 기본감시폴더 = $@"{저장.기본출력폴더}감시\";
                foreach (FileInfo 파일 in new DirectoryInfo(기본감시폴더).GetFiles ("*.*"))
                {
                    try{ 파일.Delete(); }
                    catch (Exception) {}
                }
                string 감시폴더 = $"{기본감시폴더}{상태.groupKey}.단계01";
                string 출력내용 = $"{상태.고객사}|{상태.groupKey}|{상태.출력폴더}"; 
                File.WriteAllText(감시폴더, 출력내용, Encoding.GetEncoding(949));

                var 완료고객번호 = new ConcurrentDictionary<string, byte>();
                var 오류고객번호 = new ConcurrentDictionary<string, byte>();

                txt전체cnt.Text = string.Format("{0:#,##0}", 레코드s.Count);
                int 현재완료cnt = 레코드s.Count - 전체진행대상.Count; // 이미 이전 단계에서 완료된 순수 건수
                txt완료cnt.Text = string.Format("{0:#,##0}", 현재완료cnt);
                int 정상cnt = 현재완료cnt;
                int 오류cnt = 0;

                // ★ 핵심: 03 서식구분은 무조건 완료 처리하여 Dictionary에 미리 담아둡니다.
                foreach (var rec in 자동완료대상)
                {
                    완료고객번호.TryAdd(rec.고객번호, 0);
                    rec.상태 = "약관완료";
                    정상cnt++; // 정상 처리된 것으로 간주하여 카운트 증가
                }


                // 한컴 컨트롤이 초기화되지 않았을 때만 딱 한 번 실행
                if (Ax02 == null)
                {
                    Ax02 = new AxHwpCtrl[상태.앱실행수];
                    for (int i = 0; i < 상태.앱실행수; i++)
                    {
                        int idx = i;
                        this.Invoke(new Action(() =>
                        {
                            Ax02[idx] = new AxHwpCtrl();
                            this.Controls.Add(Ax02[idx]); // 부모 폼에 등록해야 핸들이 유지됨
                            Ax02[idx].Visible = false;
                            Ax02[idx].Width = 0;
                            Ax02[idx].Height = 0;

                            // 핸들 강제 생성
                            if (!Ax02[idx].IsHandleCreated) {
                                IntPtr dummy = Ax02[idx].Handle;
                            }

                            Ax02[idx].CreateControl();
                            Ax02[idx].RegisterModule("FilePathCheckDLL", "FilePathCheckerModuleExample");
                        }));
                    }
                }
                else
                {
                    // 이미 생성된 경우, 혹시 모르니 모든 컨트롤을 깨끗이 비우고 시작
                    for (int i = 0; i < 상태.앱실행수; i++)
                    {
                        if (Ax02[i] != null) {
                            this.Invoke(new Action(() => Ax02[i].Clear(1)));
                        }
                    }
                }

                GridView mainView = grd작업리스트.MainView as GridView;
                int mainViewRowHandle = mainView.FocusedRowHandle;
                GridView sublView = mainView.GetDetailView(mainViewRowHandle, 0) as GridView;

                var semaphore = new SemaphoreSlim(상태.앱실행수);
                var tasks = new List<Task>();

                var 빈슬롯큐 = new ConcurrentQueue<int>(Enumerable.Range(0, 상태.앱실행수));
                int[] 컨트롤사용횟수 = new int[상태.앱실행수];
                
                int[] 인수_대_레코드Idx = Enumerable.Repeat(-1, 상태.앱실행수).ToArray();
                var 레코드목록 = 생성대상목록;
                int 전체레코드cnt = 레코드목록.Count();
                               

                var 안전한레코드목록 = new ConcurrentDictionary<int, 레코드>(
                                    레코드목록.Select((r, idx) => new KeyValuePair<int, 레코드>(idx, r)));

                string 쿼리경로 = System.IO.Path.GetDirectoryName(상태.입력폴더); // d:\Easy약관Plus\List\@완료\#KB_251001_1037529298\251001_1037529298_#KB_Lngtrm(1)_M(목차).dat.Re
                DirectoryInfo 경로정보 = new DirectoryInfo(쿼리경로);
                string 쿼리파일 = System.IO.Path.Combine(쿼리경로, $"{경로정보.Name}.sql");

                //쿼리파일열기(쿼리파일);

                for (int i = 1; i <= 상태.앱실행수; i++)
                {
                    string 완료파일 = string.Format("{0}{1}\\F{1}.약관완료", 상태.출력폴더, i);
                    
                    if (File.Exists(완료파일))
                    {
                        try
                        {
                            File.Delete(완료파일);
                        }
                        catch (Exception) {}
                    }
                }


                for (int i = 0; i < 전체레코드cnt; i++)
                {
                    레코드 레코드 = 레코드목록[i];
                    string 메세지 = "";
                    await semaphore.WaitAsync(); // 슬롯 확보

                    int 인수 = -1;

                    if (!빈슬롯큐.TryDequeue(out 인수))
                    {
                        // 큐에서 꺼내기 실패 시 (이론상 발생하지 않으나 방어코드)
                        semaphore.Release();
                        i--; // 현재 레코드 인덱스를 되돌려 다시 시도하게 함
                        continue;
                    }

                    인수_대_레코드Idx[인수] = i;

                    var task = Task.Run(async () =>
                    {
                        try
                        {
                            // [수정핵심 2-1] 할당받은 HWP 컨트롤이 100번 넘게 일했다면 찌꺼기를 날리고 새 컨트롤로 교체 (리사이클)
                            if (컨트롤사용횟수[인수] >= 100)
                            {
                                this.BeginInvoke(new Action(() =>
                                {
                                    로그내용 = string.Format("{0:yy.MM.dd HH:mm:ss} [시스템-알림] HWP 컨트롤(Ax02[{1}]) 사용 횟수 100회 도달. 메모리 최적화를 위해 새 객체로 교체합니다.", DateTime.Now, 인수);
                                    로그기록(로그내용, 1);
                                }));

                                this.Invoke(new Action(() =>
                                {
                                    // 1. 문서 비우기
                                    try { Ax02[인수].Clear(1); } catch { }

                                    // ★ 2. 알맹이(COM 원본)를 미리 다른 변수에 빼두기 (핵심!)
                                    object ocx = null;
                                    try { ocx = Ax02[인수].GetOcx(); } catch { }

                                    // 3. 폼(화면)에서 컨트롤 떼어내기
                                    this.Controls.Remove(Ax02[인수]);

                                    // ★ 4. 껍데기(WinForms 래퍼) 먼저 정상적으로 폐기
                                    try { Ax02[인수].Dispose(); } catch { }

                                    // ★ 5. 미리 빼둔 알맹이(COM 객체) 완전 숨통 끊기
                                    if (ocx != null)
                                    {
                                        try
                                        {
                                            int refCount;
                                            do { refCount = System.Runtime.InteropServices.Marshal.ReleaseComObject(ocx); } while (refCount > 0);
                                        }
                                        catch { }
                                    }

                                    // 6. 맑고 깨끗한 새 컨트롤 생성 및 부착
                                    Ax02[인수] = new AxHwpCtrl();
                                    this.Controls.Add(Ax02[인수]);
                                    Ax02[인수].Visible = false;
                                    Ax02[인수].Width = 0;
                                    Ax02[인수].Height = 0;
                                    Ax02[인수].CreateControl();
                                    Ax02[인수].RegisterModule("FilePathCheckDLL", "FilePathCheckerModuleExample");
                                }));

                                컨트롤사용횟수[인수] = 0; // 횟수 초기화
                                GC.Collect(); // 찌꺼기 메모리 즉시 청소
                                GC.WaitForPendingFinalizers();
                            }

                            컨트롤사용횟수[인수]++; // 이 컨트롤의 사용 횟수 1 증가

                            // 실제 약관 생성
                            메세지 = await 개별약관생성(상태, 레코드, Ax02[인수], 인수);
                        }
                        catch (Exception ee)
                        {
                            this.BeginInvoke(new Action(() =>
                            {
                                로그내용 = string.Format("{0:yy.MM.dd HH:mm:ss} [ERROR]개별약관생성 관련오류 : {1}", DateTime.Now, ee.Message);
                                로그기록(로그내용, 1);
                            }));
                        }
                        finally
                        {
                            try
                            {
                                int 해제인수 = -1;
                                int 레코드Idx = -1;

                                if (!string.IsNullOrEmpty(메세지))
                                {
                                    string[] 메세지분리 = 메세지.Split('|').Select(x => x.Trim()).ToArray();
                                    if (메세지분리.Length >= 3)
                                    {
                                        if (!int.TryParse(메세지분리[0], out 해제인수))
                                        {
                                            this.BeginInvoke(new Action(() =>
                                            {
                                                로그내용 = string.Format("{0:yy.MM.dd HH:mm:ss} [ERROR]해제인수 파싱실패 : '{1}'", DateTime.Now, 메세지분리[0]);
                                                로그기록(로그내용, 1);
                                            }));
                                            // 파싱에 실패해도 현재 스레드가 잡고 있는 '인수'를 강제 지정 (슬롯 유실 방지)
                                            해제인수 = 인수;
                                        }
                                        else
                                        {
                                            if (해제인수 >= 0 && 해제인수 < 상태.앱실행수)
                                            {
                                                레코드Idx = 인수_대_레코드Idx[해제인수];
                                                // 기존 리스트 방식 삭제 (사용가능한인수[해제인수] = 0; 삭제됨)
                                                인수_대_레코드Idx[해제인수] = -1;
                                            }
                                            else
                                            {
                                                this.BeginInvoke(new Action(() =>
                                                {
                                                    로그내용 = string.Format("{0:yy.MM.dd HH:mm:ss} [ERROR]해제인수가 범위를 벗어남 : {1}", DateTime.Now, 해제인수);
                                                    로그기록(로그내용, 1);
                                                }));
                                                해제인수 = 인수;
                                            }
                                        }

                                        string 반환코드 = 메세지분리[1];
                                        string tmp고객번호 = 메세지분리[2];

                                        if (레코드Idx >= 0 && 안전한레코드목록.TryGetValue(레코드Idx, out var rec))
                                        {
                                            if (반환코드 == "SCU" && tmp고객번호 == rec.고객번호)
                                            {
                                                // 값으로는 의미 없는 0 (byte)을 넣습니다.
                                                완료고객번호.TryAdd(rec.고객번호, 0);
                                                var 새로운레코드 = rec;
                                                새로운레코드.상태 = "약관완료";
                                                안전한레코드목록.TryUpdate(레코드Idx, 새로운레코드, rec);
                                                Interlocked.Increment(ref 정상cnt);
                                            }
                                            else
                                            {
                                                오류고객번호.TryAdd(rec.고객번호, 0);
                                                var 새로운레코드 = rec;
                                                새로운레코드.상태 = "약관오류";
                                                안전한레코드목록.TryUpdate(레코드Idx, 새로운레코드, rec);
                                                Interlocked.Increment(ref 오류cnt);
                                            }
                                        }
                                        else
                                        {
                                            this.BeginInvoke(new Action(() =>
                                            {
                                                로그내용 = string.Format("{0:yy.MM.dd HH:mm:ss} [ERROR]레코드Idx 유효하지 않음 : 해제인수={1}, 레코드Idx={2}, 메세지='{3}'",
                                                    DateTime.Now, 해제인수, 레코드Idx, 메세지);
                                                로그기록(로그내용, 1);
                                            }));
                                        }
                                    }
                                }
                                else
                                {
                                    this.BeginInvoke(new Action(() =>
                                    {
                                        로그내용 = string.Format("{0:yy.MM.dd HH:mm:ss} [ERROR]빈 메세지 수신", DateTime.Now);
                                        로그기록(로그내용, 1);
                                    }));
                                    해제인수 = 인수; // 메세지가 비어있어도 슬롯은 반드시 반납해야 하므로
                                }

                                this.BeginInvoke(new Action(() =>
                                {
                                    if (레코드Idx >= 0)
                                    {
                                        sublView.SetRowCellValue(레코드Idx, "확인", true);
                                    }
                                    txt완료cnt.Text = string.Format("{0:#,##0}", 정상cnt);
                                    txt오류cnt.Text = string.Format("{0:#,##0}", 오류cnt);

                                    if (전체레코드cnt > 0)
                                    {
                                        int 계산 = Convert.ToInt32((double)정상cnt / (double)전체레코드cnt * 100);
                                        barHwp변환진행.EditValue = 계산;
                                    }

                                    string 성공문구 = 메세지.Contains("|SCU|") == true ? "[약관생성완료]" : "[약관생성오류]";
                                    로그내용 = string.Format("{0:yy.MM.dd HH:mm:ss} {1} {2}", DateTime.Now, 성공문구, 메세지);
                                    로그기록(로그내용, 1);
                                }));

                                // 작업이 끝난 슬롯을 큐(Queue) 맨 뒤에 다시 줄을 세움 (가장 안전한 반납)
                                if (해제인수 == -1) 해제인수 = 인수;
                                빈슬롯큐.Enqueue(해제인수);
                            }
                            catch (Exception eFinal)
                            {
                                this.BeginInvoke(new Action(() =>
                                {
                                    로그내용 = string.Format("{0:yy.MM.dd HH:mm:ss} [ERROR]오류 예외 : {1}", DateTime.Now, eFinal.ToString());
                                    로그기록(로그내용, 1);
                                }));

                                // 심각한 에러가 났어도 다음 루프 진행을 위해 슬롯은 반드시 반환
                                빈슬롯큐.Enqueue(인수);
                            }
                            finally
                            {
                                semaphore.Release(); // 무조건 제일 마지막에 락 해제
                            }
                        }
                    });
                    tasks.Add(task);
                }
                await Task.WhenAll(tasks); // 모든 작업 완료 대기

                //쿼리파일닫기();

                상태.처리Step = "03";

                try
                {
                    // 모든데이터 처리 후 실행
                    // ======================================================
                    // 데이터 처리가 완료 되면 테이블에 업데이트를 한다.
                    // ======================================================

                    List<string> 완료고객리스트 = 완료고객번호.Keys.ToList();
                    List<string> 오류고객리스트 = 오류고객번호.Keys.ToList();
                    // 100개 단위로 
                    int 묶음크기 = 100;

                    for (int i = 0; i < 완료고객번호.Count; i += 묶음크기)
                    {
                        int 자를크기 = Math.Min(묶음크기, 완료고객리스트.Count - i);
                        List<string> 복사 = 완료고객리스트.GetRange(i, 자를크기);

                        // 파라미터를 담을 리스트 생성
                        List<SqlParameter> paramList = new List<SqlParameter>();

                        // 1. 공통 파라미터 (groupKey, 처리구분) 추가
                        paramList.Add(new SqlParameter("@groupKey", 레코드목록[0].groupKey));
                        paramList.Add(new SqlParameter("@처리구분", 레코드목록[0].처리구분));

                        // 2. IN 절에 들어갈 동적 파라미터(@p0, @p1, @p2 ...) 생성
                        List<string> inClauseParamNames = new List<string>();
                        for (int j = 0; j < 복사.Count; j++)
                        {
                            string paramName = $"@p{j}";
                            inClauseParamNames.Add(paramName);
                            paramList.Add(new SqlParameter(paramName, 복사[j]));
                        }

                        // 3. IN (@p0, @p1, @p2 ...) 형태의 문자열 조립
                        string inClause = string.Join(", ", inClauseParamNames);

                        // 4. SQL 문자열 생성 (값 대신 파라미터 이름 삽입)
                        string SqlStr = $@"
                            UPDATE data
                            SET 약관작업 = '약관완료', 
                                PDF변환작업 = 'PDF변환완료',
                                -- [핵심 추가] 통약관(전체약관경로가 존재하는 경우)이면 병합 과정이 없으므로 즉시 완료 처리
                                PDF병합작업 = CASE WHEN 전체약관경로 IS NOT NULL AND 전체약관경로 <> '' THEN 'PDF병합완료' ELSE PDF병합작업 END
                            WHERE groupKey = @groupKey AND 
                                  처리구분 = @처리구분 AND
                                  고객Key IN ({inClause})
                        ";

                        // 5. DBUpdate 호출 시 paramList를 배열로 변환하여 전달
                        if (await 실행(() => DBUpdate(SqlStr, 상태.dataTable접속, paramList.ToArray()), "약관생성(DBUpdate)01") == "실패")
                        {
                            throw new Exception("ER00_약관생성(DBUpdate)01 오류, 로그참조");
                        }
                    }
                    // 원래는 F1에서 PDF변환작업 = 'PDF변환완료' 업데이트를 하는게 맞으나
                    // 특별약관이 없는 것 때문에 약관만드는 곳에서 함.

                    for (int i = 0; i < 오류고객번호.Count; i += 묶음크기)
                    {
                        int 자를크기 = Math.Min(묶음크기, 오류고객리스트.Count - i);
                        List<string> 복사 = 오류고객리스트.GetRange(i, 자를크기);

                        List<SqlParameter> paramList = new List<SqlParameter>();
                        paramList.Add(new SqlParameter("@groupKey", 레코드목록[0].groupKey));
                        paramList.Add(new SqlParameter("@처리구분", 레코드목록[0].처리구분));

                        List<string> inClauseParamNames = new List<string>();
                        for (int j = 0; j < 복사.Count; j++)
                        {
                            string paramName = $"@p{j}";
                            inClauseParamNames.Add(paramName);
                            paramList.Add(new SqlParameter(paramName, 복사[j]));
                        }

                        string inClause = string.Join(", ", inClauseParamNames);

                        string SqlStr = $@"
                            UPDATE data
                            SET 약관작업 = '약관오류', PDF변환작업 = 'PDF변환오류'
                            WHERE groupKey = @groupKey AND 
                                  처리구분 = @처리구분 AND
                                  고객Key IN ({inClause})
                        ";

                        if (await 실행(() => DBUpdate(SqlStr, 상태.dataTable접속, paramList.ToArray()), "약관생성(DBUpdate)02") == "실패")
                        {
                            throw new Exception("ER00_약관생성(DBUpdate)02 오류, 로그참조");
                        }
                    }
                    //병렬작업하는 단계 : 약관작업 => PDF변환작업 => PDF병합작업 
                    //병렬작업이 종료하면 일괄업데이트 한다.

                }
                catch (Exception ex)
                {
                    throw new Exception($"[치명적 오류] 약관생성 완료 후 DB 업데이트 실패: {ex.Message}");
                }

                this.Invoke(new Action(() =>
                {
                    로그내용 = string.Format("{0:yy.MM.dd HH:mm:ss} [약관생성작업 완료] === 전체약관 === : {1:000000}", DateTime.Now, 레코드목록.Count());
                    로그기록(로그내용, 1);
                    로그내용 = string.Format("{0:yy.MM.dd HH:mm:ss} [약관생성작업 완료] === 완료약관 === : {1:000000}", DateTime.Now, 완료고객번호.Count);
                    로그기록(로그내용, 1);
                    로그내용 = string.Format("{0:yy.MM.dd HH:mm:ss} [약관생성작업 완료] === 오류약관 === : {1:000000}", DateTime.Now, 오류고객번호.Count);
                    로그기록(로그내용, 1);

                }));

            }
            catch (Exception ex)
            {
                실행여부 = "실패";
                로그내용 = string.Format("{0:yy.MM.dd HH:mm:ss} [오류] {1}", DateTime.Now, ex.Message );
                로그기록(로그내용, 1);
            }
            finally
            {
                for (int i = 1; i <= 상태.앱실행수; i++)
                {
                    //F1.약관완료, F2.약관완료.... F8.약관완료  End of File를 생성해서 F1~F8 약관생성완료를 전달한다.
                    string 완료파일 = string.Format("{0}{1}\\F{1}.약관완료", 상태.출력폴더, i);
                    File.WriteAllText(완료파일, 상태.출력폴더, Encoding.GetEncoding(949));
                }
            }


            return 실행여부;
        }

        // ==========================================================================================================
        // 03 - 02 : 기본_H,T,S,I, 목차 파일 저장 
        //           상품별 해당 파일을 미리저장하여 DB 과접속을 사전에 줄일려는 목적임
        // ==========================================================================================================
        private async Task<string> 기본약관PDF생성(현재상태 상태, 레코드 레코드)
        {
            string 실행여부 = "성공";
            int 재시도 = 0;
            List<string> tmp분류 = new List<string>();
            while (true)
            {
                try
                {
                    string 상품검색Key = string.Format("{0}{1}{2}", 레코드.상품코드, 레코드.개정일, 레코드.종분류코드);
                    string 테이블인수 = "";

                    if (레코드.개정일 != "" && 레코드.개정일.Length >= 2)
                    {
                        int 현재년도 = Convert.ToInt32(string.Format("{0:yy}", DateTime.Now));
                        int 개정년도 = Convert.ToInt32(레코드.개정일.Substring(0, 2));

                        //개정일이 없는 건 0000으로 기본세팅 AIG손해보험, 롯데손해보험
                        if (개정년도 != 0 && 개정년도 < 현재년도)
                        {
                            테이블인수 = "_" + 레코드.개정일.Substring(0, 2);
                            // 개정년도가 현재년도 보다 작으면 예전 Tabel을 가져온다 ex)담보MZ_22
                            // 아니면 기본테이블                                     ex)담보MZ
                        }
                    }

                    레코드.테이블인수 = 테이블인수;
                    if (저장.상품별세션.ContainsKey(상품검색Key))
                        return 실행여부;

                    var PdfHwpSave = new List<(string 검색조건, string 저장명)>();
                    string tmp검색조건 = "";
                    string 검색필드 = "담보PK,고객사코드,상품코드,개정일,담보분류,담보명,담보코드,대표담보코드,세션,구분,형,종,권,편,장,절,seq,분류01,분류02,분류03,분류04,실손담보,일자,형식";
                    using (SqlConnection AdoNet = new SqlConnection(저장.약관DB))
                    {
                        await AdoNet.OpenAsync();

                        // 라이나손해보험은 독립약관이 보통약관, 특별약관, 제도성약관으로 구분되어 있어 예외처리로 한다.
                        // 세션이 6DB이면 독립약관 특별담보코드 이다 따라서 아래쪽에서 독립특약 (보통약관, 제도성약관)을 출력한다.
                        // 담보코드(세션이 6DB)가 없으면 독립약관(보통약관,제도성약관)을 출력하지 않는다.
                        string SqlStr = "";

                        // DB화재만 H,B,T이 '권'정보로 여러개로 나누어 진다. 검색필드를 담보분류에서 권으로 변경한다.
                        string 검색필드명 = "담보분류";
                        if (상태.고객사 == "DB" && 레코드.발송구분 != "03") // 이메일이 아니면
                        {  
                            검색필드명 = "권";
                        }

                        SqlStr = $@"
                            SELECT {검색필드}
                            FROM 담보{상태.고객사}{테이블인수}
                            WHERE ',' + 상품코드 + ',' LIKE '%,{레코드.상품코드},%' AND
                                    개정일 = '{레코드.개정일}' AND
                                    {검색필드명} = '{레코드.종분류코드}' AND 
                                    담보 <> '' AND
                                    (세션 LIKE 'P%' OR 구분 = '목차기본타이틀')
                            ORDER BY 일련번호
                            ";

                        SqlCommand sqlcmd = new SqlCommand(SqlStr, AdoNet);
                        sqlcmd.CommandTimeout = 0;
                        using (SqlDataReader reader = await sqlcmd.ExecuteReaderAsync())
                        {
                            // XXXXX_01_01.pdf, XXXXX_03_01.pdf
                            // 보통약관(H), 제도성약관(T), 목차(IX) 생성하는 부분

                            while (await reader.ReadAsync())
                            {
                                string 형식 = reader["형식"].ToString().ToUpper();
                                if (!string.IsNullOrWhiteSpace((string)reader["분류02"]))
                                {
                                    tmp분류.Add(reader["분류02"].ToString());
                                }
                            
                                string 세션파일명 = "";
                                if (형식 == "HWP")
                                {
                                    세션파일명 = string.Format("{0}상품별세션\\{1}_IX_01.hwp", 레코드.출력폴더, 상품검색Key);   //ex) 경로\상품코드+개정일+담보분류_IX_01.pdf  - 기본
                                }
                                else
                                {
                                    //중요(라이나손해보험) - 독립약관인 상품은 H,B,T 다음에 별도로 독립약관H,독립약관B,독립약관T 출력한다.
                                    //그런데 독립약관상품중에서 독립약관이면서 독립약관담보가 존재하지 않으면 출력하지 않는다.
                                    //기본약관PDF생성 프로시져에서는 상품별로 관리 하기 때문에 일단 *.PDF로 정의된 파일은 모두 출력한다.
                                    //필요여부는 개별약관생성 프로시져에서 결정해서 독립약관을 출력한다.
                                    //참조(검색) : 고객별 H,T, 목차 복사, 라이나손해보험(별도독립약관)
                                    bool 독립약관기본 = false;
                                    // 독립_X, 독립_Y
                                    if (reader["담보코드"].ToString().Contains("독립") &&
                                        reader["세션"].ToString().Substring(0, 1).ToUpper() == "P")
                                    {
                                        독립약관기본 = true;
                                    }

                                    string 장 = "01";
                                    string 독립특약출력순서 = "";
                                    int 출력인수 = Convert.ToInt32(reader["세션"].ToString().Substring(2)); // ex) P01

                                    if (상태.고객사 == "CH" && 독립약관기본)
                                    {
                                        장 = (reader["장"].ToString() == "XX") ? "00" : reader["장"].ToString();   //독립특약의 '장'구분

                                        if (reader["세션"].ToString() == "P06")
                                            독립특약출력순서 = "01";   //독립약관 H
                                        else if (reader["세션"].ToString() == "P08")
                                        {
                                            독립특약출력순서 = "03"; //독립약관 T
                                            출력인수 = 6;  // 정렬순서를 맞쳐 병합을하기 위해 강제로 08 => 06으로 변경한다.
                                        }

                                        // 초기셋팅할때는 1개만 존재했으나(그래서 '장'구분이 없었음) 지금은 여러개의 독립특약이 존재해서 '장'으로 구분
                                        // 초기는 '장'구분값에 XX, 현재는 01,02,03으로 저장
                                        // 독립특약은 네이밍룰이 다름 : 상품코드_출력인수(세션)_장_독립특약출력순서(01:H, 02:B, 03:T)

                                        세션파일명 = string.Format("{0}상품별세션\\{1}_{2:00}_{3}_{4}.pdf", 레코드.출력폴더, 상품검색Key, 출력인수, 장, 독립특약출력순서); //ex) 경로\상품코드_출력인수(세션)_장_독립특약출력순서(01:H, 02:B, 03:T)
                                    }
                                    else
                                    {
                                        세션파일명 = string.Format("{0}상품별세션\\{1}_{2:00}_01.pdf", 레코드.출력폴더, 상품검색Key, 출력인수); //ex) 경로\상품코드_01_01.pdf  - 기본
                                    }
                                }
                                tmp검색조건 = string.Join("|", reader["담보PK"], reader["상품코드"], reader["개정일"], reader["담보분류"], reader["담보코드"], reader["세션"], reader["형"], reader["종"], reader["권"], reader["편"], reader["장"], reader["절"], reader["형식"]);
                                PdfHwpSave.Add((tmp검색조건, 세션파일명));
                               
                            }
                        }

                        //PDF파일을 빨리 저장하기 위해 먼저 파일명을 저장하고 
                        //키값을 생성해서 해당 담보만 저장한다. 'CommandBehavior.SequentialAccess' 옵션을 설정하면 빠르게 저장가능 하다
                        //단 조건은 필드크기가 순차적으로 되어 있어야 한다.
                        foreach (var(검색조건, 저장명) in PdfHwpSave)
                        {
                            string[] 조건 = 검색조건.Split('|').Select(x => x.Trim()).ToArray(); ;

                            SqlStr = $@"
                                SELECT 담보
                                FROM 담보{상태.고객사}{테이블인수}
                                WHERE 담보PK = '{조건[0]}'
                                        AND ',' + 상품코드 + ',' LIKE '%,{조건[1]},%'
                                        AND 개정일 = '{조건[2]}'
                                        AND 담보분류 = '{조건[3]}'
                                        AND 담보코드 = '{조건[4]}'
                                        AND 세션 = '{조건[5]}'
                                        AND 형 = '{조건[6]}'
                                        AND 종 = '{조건[7]}'
                                        AND 권 = '{조건[8]}'
                                        AND 편 = '{조건[9]}'
                                        AND 장 = '{조건[10]}'
                                        AND 절 = '{조건[11]}'
                            ";


                            string 형식 = 조건[12];
                            SqlCommand cmdPdf = new SqlCommand(SqlStr, AdoNet);
                            using (SqlDataReader reader = await cmdPdf.ExecuteReaderAsync(CommandBehavior.SequentialAccess))
                            {
                                if (await reader.ReadAsync())
                                {
                                    if (형식 == "HWP")
                                    {
                                        using (FileStream 세션fsRW = new FileStream(저장명, FileMode.Create, FileAccess.Write, FileShare.None))
                                        {
                                            byte[] DBpdfbyte = Convert.FromBase64String(reader["담보"].ToString());
                                            await 세션fsRW.WriteAsync(DBpdfbyte, 0, DBpdfbyte.Length);
                                            await 세션fsRW.FlushAsync();
                                        }
                                    } else
                                    {
                                        using (FileStream fs = new FileStream(저장명, FileMode.Create, FileAccess.Write, FileShare.None, 81920, useAsync: true))
                                        using (FromBase64Transform transform = new FromBase64Transform(FromBase64TransformMode.IgnoreWhiteSpaces))
                                        using (TextReader tr = reader.GetTextReader(0)) // 담보 컬럼
                                        {
                                            char[] charBuffer = new char[81920];
                                            byte[] outputBuffer = new byte[81920];

                                            int charsRead;
                                            while ((charsRead = await tr.ReadBlockAsync(charBuffer, 0, charBuffer.Length)) > 0)
                                            {
                                                byte[] inputBytes = Encoding.ASCII.GetBytes(charBuffer, 0, charsRead);
                                                int bytesTransformed = transform.TransformBlock(inputBytes, 0, inputBytes.Length, outputBuffer, 0);
                                                if (bytesTransformed > 0)
                                                    await fs.WriteAsync(outputBuffer, 0, bytesTransformed);
                                            }

                                            byte[] finalBytes = transform.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
                                            if (finalBytes.Length > 0)
                                                await fs.WriteAsync(finalBytes, 0, finalBytes.Length);
                                        }
                                    }
                                }
                            }
                            로그내용 = string.Format("{0:yy.MM.dd HH:mm:ss} [약관생성]약관기본생성완료 : 보통,제도성,목차등 파일을 저장 - 상품코드[{1}] ", DateTime.Now, System.IO.Path.GetFileNameWithoutExtension(저장명));
                            로그기록(로그내용, 1);
                        }

                    }
                    string 결합 = string.Join("|", tmp분류);
                    저장.상품별세션.Add(상품검색Key, 결합);
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
                    throw;
                }
            }
            
            return 실행여부;
        }
        // ==========================================================================================================
        // 03 - 03 : 특별담보, 목차, 별표 생성   
        // 
        // ==========================================================================================================
        private async Task Hwp초기화(AxHwpCtrl hwp)
        {
            this.Invoke(new Action(() => {
                hwp.Clear(1);
            }));
            // 컨트롤이 내부적으로 정리될 시간을 줍니다.
            await Task.Delay(100);
        }

        private async Task<string> 개별약관생성(현재상태 상태, 레코드 레코드, AxHwpCtrl Ax02, int 인수)
        {
            메소드명 = "개별약관생성";
            string 메세지 = "";
            int 담보갯수 = 0;

            //통약관이 존재하면 약관을 생성하지 않는다.
            if (!string.IsNullOrWhiteSpace(레코드.전체약관경로))
            {
                메세지 = string.Format("{0}|SCU|{1}| 개별약관생성완료(담보갯수[{2:000}])완료", 인수, 레코드.고객번호, 담보갯수);
                return 메세지;
            }
            
            try
            {
                using (SqlConnection AdoNet = new SqlConnection(저장.약관DB))
                {
                    await AdoNet.OpenAsync();

                    //담보코드에 '*'를 표시하면 모든 담보를 출력한다.(통약관)
                    List<string> 담보분리 = new List<string>();
                    if (레코드.담보코드.Contains("*"))
                    {
                        담보분리.Add("*");
                    } else
                    {
                        담보분리 = 레코드.담보코드
                            .Split(new[] { "|" }, StringSplitOptions.None)
                            .Where(x => !string.IsNullOrWhiteSpace(x))
                            .ToList();
                    }

                    string tmp담보key = "";
                    if (담보분리.Count > 0)
                    {
                        담보갯수 = 담보분리.Count;
                        tmp담보key = $"'{string.Join("','", 담보분리)}'"; //ex) 1234|5678|6543  ==> '1234','5678','6543' 담보코드검색 쿼리에서 In 인수로 사용
                    }
                    else
                    {
                        tmp담보key = "'없음'"; // 선택담보가 없는 경우 
                    }
                    string 출력폴더 = string.Format("{0}{1}\\", 레코드.출력폴더, 인수 + 1);
                    //string 저장파일명Pdf = string.Format("{0}{1}.Pdf", 출력폴더, 레코드.파일명); // 경로포함됨
                    //string 저장파일명Hwp = string.Format("{0}{1}.hwp", 출력폴더, 레코드.파일명);
                    bool 목차존재여부 = false;
                    int 총구역 = -1, 현재구역 = -1, 쪽 = -1, 단 = -1, 줄 = -1, 칸 = -1;
                    short 수정 = -1;
                    string ctrlname = "";


                    // *** 구분 구성값 ***
                    // *약관   : 보통약관, 제도성약관, 별표, 색인
                    // *타이틀 : 보통약관기본타이틀(예외), 특별약관기본타이틀, 보통담보타이틀(예외), 특별담보타이틀, 특별담보그룹타이틀, 별표기본타이틀 등...  ex) 엑셀에서 하늘색 : 기본타이틀, 회색 : 담보타이틀
                    // *담보   : 보통담보, 특별담보

                    // ====================================================================
                    //       해당상품 검색
                    // ====================================================================
                    string 검색항목 = "고객사코드, 상품코드, 개정일, 담보분류, 담보명, 담보코드, 대표담보코드, 세션, 구분, 형, 종, 권, 편, 장, 절, 분류01, 분류02, 분류03, 분류04, 실손담보, 담보, 일련번호";
                    string SqlStr = "";
                    string 검색추가문구 = (레코드.고객사 == "DB") ? "" : $"AND 담보분류 = '{레코드.종분류코드}'";

                    SqlStr = $@"
                        SELECT {검색항목}, CONCAT(형,종,권,편,장,절) AS 세션Key
                        FROM 담보{레코드.고객사}{레코드.테이블인수}
                        WHERE ',' + 상품코드 + ',' LIKE '%,{레코드.상품코드},%' 
                                AND 개정일 = '{레코드.개정일}' 
                                AND 세션 NOT LIKE 'P%' 
                        {검색추가문구} 
                        ORDER BY 일련번호
                        ";

                   
                    //오류시 쿼리검색
                    //쿼리기록(쿼리내용); //검수를 위해 쿼리를 기록한다.

                    DataSet ds02 = new DataSet();
                    DataTable table = new DataTable("타이틀tab");

                    using (SqlCommand cmd = new SqlCommand(SqlStr, AdoNet))
                    {
                        cmd.CommandTimeout = 0;
                        using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                        {
                            table.Load(reader); // ← 완전한 비동기는 아님 (Load는 여전히 동기) , table.LoadAsync(reader) 는 .NET 6+ 이상에서 가능
                        }
                    }
                    ds02.Tables.Add(table);

                    if (ds02.Tables["타이틀tab"].Rows.Count == 0)
                    {
                        메세지 = string.Format("{0}|LER|{1}|상품코드미존재, 상품코드:{2}, 개정일:{3}, 담보분류:{4}", 인수, 레코드.고객번호, 레코드.상품코드, 레코드.개정일, 레코드.종분류코드);
                        throw new Exception(메세지);
                    }

                    // ===========================
                    //      개별 담보 검색
                    // ===========================
                    // 기본필드를 가지고 전체적인 값을 제어한다. 0:보통약관, 1:특별약관, 2:제도성약관, 3:별표, 4:색인 (엑셀정렬순서 참조)
                    // 파일은 없고 제어만 한다.

                    DataRow[] 개별담보 = null;

                    // 통약관 (담보코드에 *를 넣으면 모든 담보내용이 출력된다.)
                    if (tmp담보key == "'*'")
                    {
                        개별담보 = ds02.Tables["타이틀tab"].Select("구분 = '특별담보'", "일련번호");
                    }
                    else
                    {
                        개별담보 = ds02.Tables["타이틀tab"].Select(string.Format("담보코드 in ({0})", tmp담보key), "일련번호");
                    }
                    int 출력담보갯수 = 개별담보.Length;

                    // ===========================
                    //      구분 = '기본' 검색
                    // ===========================
                    // 기본필드를 가지고 전체적인 값을 제어한다. 0:보통약관, 1:특별약관, 2:제도성약관, 3:별표, 4:색인 (엑셀정렬순서 참조)
                    // 파일은 없고 제어만 한다.
                    DataRow[] 기본 = ds02.Tables["타이틀tab"].Select("구분 = '기본'", "세션");
                    string 독립특약상품 = 기본[0]["실손담보"].ToString();


                    // *** 보통약관,제도성약관,별표가 여러개로 구분되는 경우 ***
                    // 별표는 담보형태로 올 수도 있고 처음 담보 셋팅 할때(필드명: 분류02) DB에 정의에 할 수 있다.
                    List<string> Tmp별표 = new List<string>();
                    string 분류02 = ""; //별표코드 저장되어 있음
                    string 세션파일명 = "";

                    // =======================================================
                    //   고객별 H,T, 목차 복사, 라이나손해보험(별도독립약관)
                    // =======================================================

                    int 보통약관Pcnt = -1;
                    int 제도성약관Pcnt = -1;
                    int 색인Pcnt = -1;
                    int 특별약관Pcnt = -1;
                    int 별표Pcnt = -1;

                    //라이나손해보험(별도독립약관)
                    List<string> 독립약관_장 = new List<string>();
                    if (레코드.고객사 == "CH")
                    {
                        var 독립약관장 = 개별담보.AsEnumerable()
                            .Where(행 => 행.Field<string>("세션") == "6DB") // '세션'이 '6DB'인 행만 필터링
                            .Select(행 => 행.Field<string>("장"))         // 필터링된 행에서 '장' 필드값만 선택
                            .Distinct()                                     // 중복된 값을 제거하여 고유한 값만 남김
                            .ToList();

                        foreach (string 장 in 독립약관장)
                        {
                            string tmp독립약관파일명 = $"{레코드.상품코드}{레코드.개정일}{레코드.종분류코드}_06_{장}";
                            독립약관_장.Add($"{tmp독립약관파일명}_01"); //독립약관H
                            독립약관_장.Add($"{tmp독립약관파일명}_03"); //독립약관T 
                        }
                    }

                    string 상품검색Key = string.Format("{0}{1}{2}", 레코드.상품코드, 레코드.개정일, 레코드.종분류코드);
                    if (저장.상품별세션.TryGetValue(상품검색Key, out 분류02))
                    {
                        string[] 분리별표 = 분류02.Split('|').Select(P => P.Trim()).ToArray();
                        for (int p = 0; p < 분리별표.Length; p++)
                        {
                            if (분리별표[p].Trim() != "")
                                Tmp별표.Add(분리별표[p]);
                        }

                        DirectoryInfo 상품별세션경로 = new DirectoryInfo(string.Format("{0}상품별세션\\", 레코드.출력폴더));
                        foreach (FileInfo 파일명 in 상품별세션경로.GetFiles(string.Format("{0}*.pdf", 상품검색Key)))
                        {
                            //독립약관인지 구분, 라이나손본 독립약관은 상품별세션에서 모두 출력을 한다.
                            //톡립약관상품중 일부는 독립약관 담보가 존재 하지 않는 상품이 있다 그런 상품은 제외
                            //또한 '장'으로 구분되어 있기 때문에 '장'에 해당하는 H,T를 출력한다.

                            if (레코드.고객사 == "CH")
                            {
                                if (파일명.Name.StartsWith($"{상품검색Key}_06")) // 독립약관상품
                                {
                                    string tmp독립상품 = System.IO.Path.GetFileNameWithoutExtension(파일명.Name); //확장자제거
                                    if (독립약관_장.IndexOf (tmp독립상품) == -1)
                                    {
                                        continue; //해당독립약관은 현재고객 상품에는 없으므로 출력제외
                                    }
                                }
                            }

                            string[] tmp상품검색 = 파일명.Name.Split('_');
                            if (tmp상품검색[0] == 상품검색Key)
                            {
                                파일명.CopyTo(string.Format("{0}{1}", 레코드.출력폴더, 파일명.Name.Replace(상품검색Key, 레코드.고객번호)), true);

                                // 목차생성이면
                                using (FileStream 고정약관PDF = new FileStream(파일명.FullName, FileMode.Open, FileAccess.Read, FileShare.Read))
                                using (PdfDocument 개별DOC = new PdfDocument(new PdfReader(고정약관PDF)))
                                {
                                    int 약관Pcnt = 개별DOC.GetNumberOfPages();
                                    switch (tmp상품검색[1])
                                    {
                                        case "01":
                                            보통약관Pcnt = 약관Pcnt;
                                            break;
                                        case "03":
                                            제도성약관Pcnt = 약관Pcnt;
                                            break;
                                        case "05":
                                            색인Pcnt = 약관Pcnt;
                                            break;
                                    }
                                }
                            }
                        }

                        string 목차파일 = string.Format("{0}상품별세션\\{1}_IX_01.hwp", 레코드.출력폴더, 상품검색Key);
                        if (new FileInfo(목차파일).Exists)
                        {
                            new FileInfo(목차파일).CopyTo(string.Format("{0}목차\\{1}", 레코드.출력폴더, new FileInfo(목차파일).Name.Replace(상품검색Key, 레코드.고객번호)), true);    // 고객번호_IX_01.pdf - 현재는 목차파일 존재하는 상황
                            목차존재여부 = true;
                        }

                    }
                    else
                    {
                        메세지 = string.Format("{0}|LER|{1}| 세션파일(H,T)미존재, 상품코드-{2}", 인수, 레코드.고객번호, 레코드.상품코드);
                        throw new Exception(메세지);
                    }



                    if (!string.IsNullOrWhiteSpace(분류02))
                    {
                        string[] 분리별표 = 분류02.Split('|').Select(P => P.Trim()).ToArray();
                        for (int p = 0; p < 분리별표.Length; p++)
                        {
                            Tmp별표.Add(분리별표[p]);
                        }
                    }

                    string 이전세션타이틀key = "";
                    string 이전타이틀key = "****************************";
                    string 현재타이틀key = "";
                    string 현재세션타이틀key = "";
                    string 이전독립Key = "";
                    string[] 이전타이틀 = new string[6];
                    Dictionary<string, int> 중복담보확인 = new Dictionary<string, int>();
                    Dictionary<string, int> 중복담보그룹타이틀확인 = new Dictionary<string, int>();
                    Dictionary<string, int> 중복독립특약별표 = new Dictionary<string, int>();
                    var hwpbyte = "";
                    DataRow[] 기본타이틀;
                    DataRow[] 담보그룹타이틀;
                    string[] tmp타이틀 = new string[6];
                    List<string> tmp목차 = new List<string>();
                    List<KeyValuePair<string, int>> 목차쪽번호 = new List<KeyValuePair<string, int>>();
                    string tmp형종권편장절 = "";
                    DataRow row;

                    await Hwp초기화(Ax02); // 묻지않고 바로 클리어 한다.
                                           // 출력담보에는 보통약관담보, 특별약관담보, 제도성약관담보, 별표 등이 올 수 있다.

                    bool 세션저장확인 = false;
                    bool 독립특약별표여부 = false;  // 메리츠화재의 독립특약별표는 독립특약을 출력 후 마지막에 별도로 출력한다.
                                                    // KB손해보험은 독립제도성약관을 독립특약을 출렫 후 마지막에 별도로 출력한다.
                    string 독립특약별표코드 = "";
                    bool 공통별표출력여부 = false;
                    DataRow[] 독립특약별표담보 = null;
                    string 검색쿼리 = "";

                    // ==================================
                    //      담보코드 존재여부 검사 
                    // ==================================

                    //보통약관에 존재하는 담보코드를 넣어주는 경우가 많아(KB손해보험) 약관갯수 체크로직에서 제외
                    //특히 실손보험같은 경우 통약관으로 보내기 때문에 담보갯수와 불일치
                    if ((상태.고객사 == "DB" || 상태.고객사 == "LT") && 담보갯수 > 0 && 담보갯수 != 출력담보갯수)
                    {
                        List<string> 미담보코드 = new List<string>();
                        List<string> 검색담보코드 = new List<string>();

                        for (int k = 0; k < 출력담보갯수; k++)
                        {
                            검색담보코드.Add((string)개별담보[k]["담보코드"]);
                        }

                        // List에 입력된 담보코드를 검색된 담보코드와 비교하여 존재하지 않으면 에러를 발생한다.
                        for (int k = 0; k < 담보갯수; k++)
                        {
                            int 담보찾기인수 = 검색담보코드.FindIndex(x => x == 담보분리[k]);
                            if (담보찾기인수 == -1)
                            {
                                미담보코드.Add(담보분리[k]);
                            }
                        }

                        if (미담보코드.Count > 0)
                        {
                            string 출력담보 = string.Join("|", 미담보코드);  // Array to String 변환
                            메세지 = string.Format("{0}|LER|{1}| 담보코드미존재, 상품코드-{2}, 담보코드-{3}", 인수, 레코드.고객번호, 레코드.상품코드, 출력담보);
                            throw new Exception(메세지);
                        }
                    }

                    for (int i = 0; i < 출력담보갯수; i++)
                    {
                        // ==================================
                        //           중복 담보 제거 
                        // ==================================
                        // 담보코드중 일부가 대표담보코드가 존재한다. 
                        // 그중 중복담보는 1번 출력한다.

                        if (!string.IsNullOrEmpty((string)개별담보[i]["대표담보코드"]))
                        {
                            string tmp대표담보코드 = 개별담보[i]["대표담보코드"].ToString();

                            if (중복담보확인.ContainsKey(tmp대표담보코드))    // 중복담보중 출력했으면 Pass
                            {
                                continue;
                            }
                            else
                            {
                                중복담보확인.Add(tmp대표담보코드, 1);           // 대표담보코드중 다음에 출력방지를 위해 저장한다.
                            }
                        }

                        // =====================================
                        //   독립특약상품 - 공통별표 출력 (1/2)
                        // =====================================
                        // 순서 ) 보통약관 - 특별약관 ( 선택특약 => 공통별표 => 독립특약 ) - 제도성약관 
                        // 즉 : 선택특약, 공통별표, 독립특약 모두 특별약관 처럼 처리한다.

                        if (독립특약상품 == "독립특약")
                        {
                            if (공통별표출력여부 == false && 개별담보[i]["실손담보"].ToString() != "")
                            {
                                //개별담보[i]["실손담보"].ToString() != "" : 독립특약 별표값이 저장 되어 있다.
                                DataRow[] 공통별표 = ds02.Tables["타이틀tab"].Select(string.Format("구분 = '공통별표' and 담보 <> ''"), "일련번호");

                                if (공통별표.Length != 0)
                                {
                                    hwpbyte = 공통별표[0]["담보"].ToString();
                                    Ax02.MovePos(3, 0, 0);
                                    Ax02.SetTextFile(hwpbyte, "HWP", "insertfile");
                                    Ax02.MovePos(3, 0, 0);

                                    row = 공통별표[0];
                                    tmp형종권편장절 = string.Join("", row["형"], row["종"], row["권"], row["편"], row["장"], row["절"]);
                                    tmp목차.Add(string.Join("|", row["일련번호"], row["담보명"], row["구분"], tmp형종권편장절));
                                }
                                공통별표출력여부 = true;
                            }
                        }

                        // ==================================
                        //           대표담보코드
                        // ==================================
                        string 현재대표담보코드 = 개별담보[i]["대표담보코드"].ToString();
                        DataRow[] 대표담보;
                        var tmp대표담보 = "";

                        if (string.IsNullOrWhiteSpace(현재대표담보코드))
                        {
                            string 현재담보코드 = 개별담보[i]["담보코드"].ToString();
                            대표담보 = ds02.Tables["타이틀tab"].Select(string.Format("담보코드 = '{0}' and 담보 <> ''", 현재담보코드), "일련번호");
                        }
                        else
                        {
                            대표담보 = ds02.Tables["타이틀tab"].Select(string.Format("대표담보코드 = '{0}' and 담보 <> ''", 현재대표담보코드), "일련번호");
                        }

                        if (대표담보.Length == 0)
                        {
                            메세지 = string.Format("{0}|LER|{1}| 대표담보미존재, 상품코드-{2}, 담보코드-{3}", 인수, 레코드.고객번호, 레코드.상품코드, 개별담보[i]["담보코드"].ToString());
                            throw new Exception(메세지);
                        }
                        tmp대표담보 = 대표담보[0]["담보"].ToString();

                        // ===========================
                        //      타이틀 셋팅 (세션)
                        // ===========================

                        //라이나생명(CH) - 독립특약01
                        // 세션 - 타이틀과 담보를 구분하기 위해 설정, 타이틀(T), 담보(DB)
                        현재세션타이틀key = string.Format("{0:00}", Convert.ToInt16(개별담보[i]["세션"].ToString().Substring(0, 1)));  //  01:보통, 02:특별, 03:제도, 04:별표, 05:색인, 06:특별보통약관, 08:특별제도성약관
                        if (레코드.고객사 == "CH" && 현재세션타이틀key == "06")
                        {
                            현재세션타이틀key = string.Format("{0}_{1}", 현재세션타이틀key, 개별담보[i]["장"].ToString());
                        }

                        //세션타이틀 추가
                        //세션이 다르면 현재 세션을 저장하고 다음세션 타이틀을 불러온다. ex) 보통약관담보를 출력하다가 특별약관 담보가 오면 보통약관담보를 저장하고 특별약관세션 타이틀을 불러온다. 

                        if (현재세션타이틀key != 이전세션타이틀key)
                        {

                            // ====================================
                            //       독립특약담보 별표출력 (1/4)
                            // ====================================
                            // 이전 독립특약담보가 존재하면 마지막으로 독립특약담보 별표를 출력한다.

                            if (독립특약별표여부)
                            {

                                hwpbyte = 독립특약별표담보[0]["담보"].ToString();
                                Ax02.MovePos(3, 0, 0);
                                Ax02.SetTextFile(hwpbyte, "HWP", "insertfile");
                                Ax02.MovePos(3, 0, 0);

                                독립특약별표여부 = false;
                                row = 독립특약별표담보[0];
                                tmp형종권편장절 = string.Join("", row["형"], row["종"], row["권"], row["편"], row["장"], row["절"]);
                                tmp목차.Add(string.Join("|", row["일련번호"], row["담보명"], row["구분"], tmp형종권편장절));
                            }


                            // ===================
                            //    담보저장 ①/3
                            // ===================
                            if (!string.IsNullOrWhiteSpace(이전세션타이틀key))
                            { // 처음이 아니면 ,1번째는 제외

                                try
                                {
                                    // PDF저장1
                                    //라이나생명(CH) - 독립특약01
                                    세션파일명 = string.Format("{0}{1}\\{2}_{3}_02.hwp", 레코드.출력폴더, 인수 + 1, 레코드.고객번호, 이전세션타이틀key); //ex) 경로\고객번호_00_02.hwp  - 멀티처리를 위해 폴더를 분리해서 저장한다.  00: H,   02: 가변
                                    세션저장확인 = Ax02.SaveAs(세션파일명, "HWP", ""); //저장파일명

                                    if (이전세션타이틀key == "02")
                                    {
                                        특별약관Pcnt = Ax02.PageCount;
                                    }

                                    await Hwp초기화(Ax02);

                                    if (세션저장확인)
                                    {
                                        //삭제7 : 먼저 파일을 저장한다 - 저장하는 동안 파일을 읽어서 PDF를 변환하는걸 방지하기 위해 저장 후 Tmp를 붙여 오류를 방지 한다.
                                        new FileInfo(세션파일명).CopyTo(string.Format("{0}\\Tmp_{1}", new FileInfo(세션파일명).DirectoryName, new FileInfo(세션파일명).Name), true);
                                        new FileInfo(세션파일명).Delete();
                                    }

                                }
                                catch (Exception e1)
                                {
                                    메세지 = string.Format("{0}|SER|{1}| 담보저장 ①/3 오류, 파일명-{2}, 오류-{3}==>{4} ", 인수, 레코드.고객번호, 세션파일명, e1.Message, e1.StackTrace);
                                    throw new Exception(메세지);
                                }
                            }

                            string[] 컬럼들1 = { "형", "종", "권", "편", "장", "절" };
                            tmp타이틀 = 컬럼들1
                                .Select(c => (개별담보[i][c]?.ToString() == "XX") ? "XX" : "00") // 00 : <===== '장' 타이틀만 존재한다는 의미(즉 타이틀이 변경된다.)
                                .ToArray();

                            // =================================================================
                            //    기본타이틀 검색 (특별약관,제도성, 별표, 색인등 기본타이틀 )
                            // =================================================================
                            // 처음 시작하는 표지가 맨처음에 나옴
                            // 세션당 기본타이틀은 1개만 존재한다.

                            기본타이틀 = ds02.Tables["타이틀tab"].Select(string.Format("구분 like '%기본타이틀' and 세션 like '{0}%' and 담보 <> ''", 현재세션타이틀key.Substring(1,1)), "일련번호");

                            if (기본타이틀.Length == 0)
                            {
                                메세지 = string.Format("{0}|LER|{1}| 기본타이틀검색실패, 쿼리 - 구분 like '%기본타이틀' and 세션 like '{2}%'", 인수, 레코드.고객번호, 현재세션타이틀key.Substring(1, 1));
                                throw new Exception(메세지);
                            }

                            // ==================================
                            //        세션 기본타이틀 출력
                            // ==================================
                            // 삽입시 맨 앞으로 하면 문서의 속성이 그대로 이어진다 즉 스타일 바탕글이 새롭게 대체 된다. ( ※※※ 매우중요 ※※※ )
                            hwpbyte = 기본타이틀[0]["담보"].ToString();  // 세션, 기본타이틀 ex) 특별약관기본타이틀
                            Ax02.MovePos(3, 0, 0);
                            Ax02.SetTextFile(hwpbyte, "HWP", "");  // 맨앞으로 
                            Ax02.MovePos(3, 0, 0);

                            row = 기본타이틀[0];
                            tmp형종권편장절 = string.Join("", row["형"], row["종"], row["권"], row["편"], row["장"], row["절"]);
                            tmp목차.Add(string.Join("|", row["일련번호"], row["담보명"], row["구분"], tmp형종권편장절));

                            이전세션타이틀key = 현재세션타이틀key;
                        }

                        string[] 컬럼들2 = { "형", "종", "권", "편", "장", "절" };
                        string[] 현재타이틀 = 컬럼들2
                            .Select(c => 개별담보[i][c]?.ToString() ?? "")
                            .ToArray();

                        현재타이틀key = 현재세션타이틀key + string.Join("", 현재타이틀);


                        // ==================================
                        //         약관 타이틀 출력 
                        // ==================================
                        if (이전타이틀key != 현재타이틀key)
                        {
                            // ====================================
                            //       독립특약담보 별표출력 (2/4)
                            // ====================================
                            // 이전 독립특약담보가 존재하면 마지막으로 독립특약담보 별표를 출력한다.

                            if (독립특약별표여부)
                            {
                                hwpbyte = 독립특약별표담보[0]["담보"].ToString();
                                if (string.IsNullOrWhiteSpace(hwpbyte))
                                {
                                    메세지 = string.Format("{0}|LER|{1}| 독립특약담보 별표 미존재 (2/4), 상품코드-{2}, 담보코드-{3}", 인수, 레코드.고객번호, 레코드.상품코드, 독립특약별표담보[0]["담보코드"].ToString());
                                    throw new Exception(메세지);
                                }

                                Ax02.MovePos(3, 0, 0);
                                Ax02.SetTextFile(hwpbyte, "HWP", "insertfile");
                                Ax02.MovePos(3, 0, 0);

                                독립특약별표여부 = false;
                                row = 독립특약별표담보[0];
                                tmp형종권편장절 = string.Join("", row["형"], row["종"], row["권"], row["편"], row["장"], row["절"]);
                                tmp목차.Add(string.Join("|", row["일련번호"], row["담보명"], row["구분"], tmp형종권편장절));

                            }

                            for (int idx = 0; idx < 현재타이틀.Length; idx++)
                            {
                                if (tmp타이틀[idx] != "XX" && 현재타이틀[idx] != "00" && (현재타이틀[idx] != 이전타이틀[idx]))
                                {
                                    // 01 00 00 00 00  - ※ 기본타이틀에 값을 기준으로 해당 필드값이 XX이면 출력하지 않는다.
                                    // 타이틀 출력
                                    // 권편장에 해당하는 타이틀을 삽입한다.
                                    // XX가 아니고 00도 아니면 출력

                                    //string 검색key = string.Format("{0}", 현재타이틀key.Substring(1, (idx + 1) * 2));
                                    // 라이나생명(CH) 독립특약이 세션값이 (1자리 =>4자리) 변경 됨
                                    // 뒷자리 12자리는(형,종,권,편,장,절 각각2자리씩)
                                    string 검색key = string.Format("{0}", 현재타이틀key.Substring(현재타이틀key.Length -12, (idx + 1) * 2)); 
                                    검색key = 검색key.PadRight(12, '0');

                                    검색쿼리 = string.Format("세션 like '{0}%' and  구분 like '%담보타이틀' and  substring(세션Key,1,{1}) = '{2}' and 담보 <> ''", 현재세션타이틀key.Substring(1,1), 검색key.Length, 검색key);   // 세션Key = CONCAT(형,종,권,편,장,절)
                                    DataRow[] 출력타이틀 = ds02.Tables["타이틀tab"].Select(검색쿼리, "일련번호");

                                    if (출력타이틀.Length != 1) 
                                    {
                                        메세지 = string.Format("{0}|LER|{1}| 담보타이틀검색실패, 상품코드-{2}, 세션-{3}", 인수, 레코드.고객번호, 레코드.상품코드, 검색key);
                                        throw new Exception(메세지);
                                    }

                                    hwpbyte = 출력타이틀[0]["담보"].ToString();
                                    Ax02.MovePos(3, 0, 0);
                                    Ax02.SetTextFile(hwpbyte, "HWP", "insertfile");
                                    Ax02.MovePos(3, 0, 0);

                                    row = 출력타이틀[0];
                                    tmp형종권편장절 = string.Join("", row["형"], row["종"], row["권"], row["편"], row["장"], row["절"]);
                                    tmp목차.Add(string.Join("|", row["일련번호"], row["담보명"], row["구분"], tmp형종권편장절));

                                }
                            }

                            이전타이틀key = 현재타이틀key;
                        }
                        Array.Copy(현재타이틀, 이전타이틀, 6); // 대분류가 바뀌면 중,소분류타이틀이 바뀐다.


                        // ====================================
                        //       독립특약담보 별표출력 (3/4)
                        // ====================================
                        // 독립별표, 독립제도성이 다른데 형종권편장절값이 같아서 출력이 않되는 경우
                        // 이전독립제도성,별도가 다르면 독립별표,독립제도성을 출력한다.

                        독립특약별표코드 = 개별담보[i]["실손담보"].ToString();  // 실손담보 필드에 독립특약별표코드가 저장 되어 있다.
                        if (!string.IsNullOrWhiteSpace(이전독립Key) && (이전독립Key != 독립특약별표코드) && 독립특약별표여부)
                        {

                            hwpbyte = 독립특약별표담보[0]["담보"].ToString();

                            if (string.IsNullOrWhiteSpace(hwpbyte))
                            {
                                메세지 = string.Format("{0}|LER|{1}| 독립특약담보 별표 미존재 (3/4), 상품코드-{2}, 담보코드-{3}", 인수, 레코드.고객번호, 레코드.상품코드, 독립특약별표담보[0]["담보코드"].ToString());
                                throw new Exception(메세지); 
                            }

                            Ax02.MovePos(3, 0, 0);
                            Ax02.SetTextFile(hwpbyte, "HWP", "insertfile");
                            Ax02.MovePos(3, 0, 0);

                            독립특약별표여부 = false;
                            row = 독립특약별표담보[0];
                            tmp형종권편장절 = string.Join("", row["형"], row["종"], row["권"], row["편"], row["장"], row["절"]);
                            tmp목차.Add(string.Join("|", row["일련번호"], row["담보명"], row["구분"], tmp형종권편장절));
                        }



                        // ==================================
                        //          담보그룹타이틀 
                        // ==================================

                        string 분류01 = 개별담보[i]["분류01"].ToString();
                        if (분류01 != "")
                        {
                            // 담보그룹타이틀중 출력했으면 Pass
                            if (중복담보그룹타이틀확인.ContainsKey(분류01) == false)
                            {
                                중복담보그룹타이틀확인.Add(분류01, 1); // 담보그룹타이틀중 다음에 출력방지를 위해 저장한다.

                                검색쿼리 = string.Format("구분 like '%담보그룹타이틀%' and substring(세션,1,1) = '{0}' and 분류01 = '{1}' and 담보 <> ''", 현재세션타이틀key.Substring(1,1), 분류01);
                                담보그룹타이틀 = ds02.Tables["타이틀tab"].Select(검색쿼리, "일련번호");

                                if (담보그룹타이틀.Length == 0)
                                {
                                    메세지 = string.Format("{0}|LER|{1}| 담보그룹타이틀검색오류, 담보그룹타이틀-{2}, 세션-{3}", 인수, 레코드.고객번호, 분류01, 현재세션타이틀key.Substring(1,1));
                                    throw new Exception(메세지);
                                }
                                else
                                {
                                    hwpbyte = 담보그룹타이틀[0]["담보"].ToString();
                                    Ax02.MovePos(3, 0, 0);
                                    Ax02.SetTextFile(hwpbyte, "HWP", "insertfile"); // (앞으로 삽입 : "insertfile")
                                    Ax02.MovePos(3, 0, 0);

                                    row = 담보그룹타이틀[0];
                                    tmp형종권편장절 = string.Join("", row["형"], row["종"], row["권"], row["편"], row["장"], row["절"]);
                                    tmp목차.Add(string.Join("|", row["일련번호"], row["담보명"], row["구분"], tmp형종권편장절));
                                }
                            }
                        }

                        // ==================================
                        //        담보삽입 
                        // ==================================
                        hwpbyte = tmp대표담보;
                        Ax02.MovePos(3, 0, 0);
                        Ax02.SetTextFile(hwpbyte, "HWP", "insertfile"); // (앞으로 삽입 : "insertfile")


                        // ==================================
                        //   목차용 쪽번호 저장
                        // ==================================

                        총구역 = -1; 현재구역 = -1;
                        쪽 = -1; 단 = -1; 줄 = -1; 칸 = -1;
                        수정 = -1;
                        ctrlname = "";

                        // 목차에서 출력할 쪽번호를 저장한다.
                        Ax02.KeyIndicator(ref 총구역, ref 현재구역, ref 쪽, ref 단, ref 줄, ref 칸, ref 수정, ref ctrlname);
                        // 동일한 항목 오류 자주 발생(대표담보코드 설정할것)
                        //목차쪽번호.Add(개별담보[i]["담보명"].ToString(), 쪽);
                        목차쪽번호.Add(new KeyValuePair<string, int>(개별담보[i]["담보명"].ToString(), 쪽));

                        Ax02.MovePos(3, 0, 0);

                        row = 대표담보[0];
                        tmp형종권편장절 = string.Join("", row["형"], row["종"], row["권"], row["편"], row["장"], row["절"]);
                        tmp목차.Add(string.Join("|", row["일련번호"], row["담보명"], row["구분"], tmp형종권편장절));

                        // =============================
                        //    독립특약별표,제도성 저장 
                        // =============================

                        if (!string.IsNullOrWhiteSpace(독립특약별표코드))
                        {

                            독립특약별표담보 = ds02.Tables["타이틀tab"].Select(string.Format("담보코드 = '{0}' and 구분 like '독립%' and 담보 <> ''", 독립특약별표코드), "일련번호");

                            if (독립특약별표담보.Length == 0)
                            {
                                메세지 = string.Format("{0}|LER|{1}| 독립특약별표담보 미존재, 담보코드-{2}", 인수, 레코드.고객번호, 독립특약별표코드);
                                throw new Exception(메세지);
                            }
                            독립특약별표여부 = true;
                            이전독립Key = 독립특약별표코드;
                        }
                        else
                        {
                            독립특약별표여부 = false;
                        }

                        // ==================================
                        //   특별약관 담보에 있는 별표 저장
                        // ==================================
                        분류02 = 개별담보[i]["분류02"].ToString();
                        if (!string.IsNullOrWhiteSpace(분류02))
                        {
                            string[] 분리별표 = 분류02.Split('|').Select(P => P.Trim()).ToArray();
                            for (int p = 0; p < 분리별표.Length; p++)
                            {
                                Tmp별표.Add(분리별표[p]);
                            }
                        }

                    }
                    // ======================================================================================================================================================

                    // =====================================
                    //   독립특약상품 - 공통별표 출력 (2/2)
                    // =====================================
                    // 순서 ) 보통약관 - 특별약관 ( 선택특약 => 공통별표 => 독립특약 ) - 제도성약관 
                    // 즉 : 선택특약, 공통별표, 독립특약 모두 특별약관 처럼 처리한다.

                    if (독립특약상품 == "독립특약" && 공통별표출력여부 == false)
                    {
                        //개별담보[i]["실손담보"].ToString() != "" : 독립특약 별표값이 저장 되어 있다.
                        DataRow[] 공통별표 = ds02.Tables["타이틀tab"].Select(string.Format("구분 = '공통별표' and 담보 <> ''"), "일련번호");
                        if (공통별표.Length != 0)
                        {
                            // MZ 공통별표가 존재하나 특별담보가 존재 하지 않아 특별약관세션에 출력하기 위해 세션 타이틀을 출력한다.
                            if (출력담보갯수 == 0)
                            {
                                기본타이틀 = ds02.Tables["타이틀tab"].Select(string.Format("구분 like '%기본타이틀' and 세션 like '{0}%' and 담보 <> ''", 2), "일련번호");
                                hwpbyte = 기본타이틀[0]["담보"].ToString();  // 세션, 기본타이틀 ex) 특별약관기본타이틀
                                Ax02.MovePos(3, 0, 0);
                                Ax02.SetTextFile(hwpbyte, "HWP", "");  // 맨앞으로 
                                Ax02.MovePos(3, 0, 0);
                            }

                            hwpbyte = 공통별표[0]["담보"].ToString();
                            Ax02.MovePos(3, 0, 0);
                            Ax02.SetTextFile(hwpbyte, "HWP", "insertfile");
                            Ax02.MovePos(3, 0, 0);

                            row = 공통별표[0];
                            tmp형종권편장절 = string.Join("", row["형"], row["종"], row["권"], row["편"], row["장"], row["절"]);
                            tmp목차.Add(string.Join("|", row["일련번호"], row["담보명"], row["구분"], tmp형종권편장절));
                        }
                        공통별표출력여부 = true;
                    }

                    // ====================================
                    //       독립특약담보 별표출력 (4/4)
                    // ====================================
                    // 이전 독립특약담보가 존재하면 마지막으로 독립특약담보 별표를 출력한다.

                    if (독립특약별표여부)
                    {

                        hwpbyte = 독립특약별표담보[0]["담보"].ToString();
                        Ax02.MovePos(3, 0, 0);
                        Ax02.SetTextFile(hwpbyte, "HWP", "insertfile");
                        Ax02.MovePos(3, 0, 0);

                        독립특약별표여부 = false;
                        row = 독립특약별표담보[0];
                        tmp형종권편장절 = string.Join("", row["형"], row["종"], row["권"], row["편"], row["장"], row["절"]);
                        tmp목차.Add(string.Join("|", row["일련번호"], row["담보명"], row["구분"], tmp형종권편장절));
                    }

                    // ==================================
                    //    담보저장 ②/3
                    // ==================================
                    // MZ는 특별담보 없이 공통별표만 출력 하는 경우 존재

                    if (출력담보갯수 > 0 || 공통별표출력여부 == true)
                    {
                        try
                        {
                            if (출력담보갯수 == 0 || 공통별표출력여부 == true)
                            {
                                세션파일명 = string.Format("{0}{1}\\{2}_02_02.hwp", 레코드.출력폴더, 인수 + 1, 레코드.고객번호); // MZ는 특별담보 없이 공통별표만 출력 하는 경우 존재
                                특별약관Pcnt = Ax02.PageCount;
                            }
                            else
                            {
                                세션파일명 = string.Format("{0}{1}\\{2}_{3}_02.hwp", 레코드.출력폴더, 인수 + 1, 레코드.고객번호, 현재세션타이틀key); //ex) 경로\고객번호_00_02.hwp  - 멀티처리를 위해 폴더를 분리해서 저장한다.
                                if (현재세션타이틀key == "02")
                                {
                                    특별약관Pcnt = Ax02.PageCount;
                                }
                            }
                            //PDF저장2
                            세션저장확인 = Ax02.SaveAs(세션파일명, "HWP", ""); //저장파일명
                            await Hwp초기화(Ax02);

                            if (세션저장확인 == true)
                            {
                                string 원본파일명 = 세션파일명;
                                string 디렉토리 = new FileInfo(원본파일명).DirectoryName;
                                string 원본이름 = new FileInfo(원본파일명).Name;

                                // 최초 대상 파일명: Tmp_aaa.hwp
                                string 대상파일명 = string.Format("{0}\\Tmp_{1}", 디렉토리, 원본이름);

                                int 최대재시도 = 3;
                                int 대기시간ms = 1000; // 이름이 바뀌므로 오래 대기할 필요 없이 1초면 충분합니다.

                                for (int i = 0; i < 최대재시도; i++)
                                {
                                    try
                                    {
                                        System.IO.File.Copy(원본파일명, 대상파일명, true);
                                        System.IO.File.Delete(원본파일명);
                                        break; // 복사 성공하면 루프 탈출
                                    }
                                    catch (System.IO.IOException) // 파일이 잠겨서 덮어쓰기 실패한 경우
                                    {
                                        if (i == 최대재시도 - 1)
                                        {
                                            throw new Exception($"파일 복사 최후 실패 (다른 프로세스 사용중): {대상파일명}");
                                        }

                                        // 실패 시 대상 파일명에 Tmp_를 한 번 더 붙여서 회피
                                        원본이름 = "Tmp_" + 원본이름;
                                        대상파일명 = string.Format("{0}\\Tmp_{1}", 디렉토리, 원본이름);
                                        // 1차 실패 후: Tmp_Tmp_aaa.hwp
                                        // 2차 실패 후: Tmp_Tmp_Tmp_aaa.hwp

                                        await Task.Delay(대기시간ms);
                                    }
                                    catch (UnauthorizedAccessException)
                                    {
                                        if (i == 최대재시도 - 1) throw new Exception($"파일 접근 권한 없음: {대상파일명}");
                                        await Task.Delay(대기시간ms);
                                    }
                                }
                            }
                        }
                        catch (Exception e1)
                        {
                            메세지 = string.Format("{0}|SER|{1}| 담보저장 ②/3 오류, 세션파일명-{2}, 오류 - {3}==>{4} ", 인수, 레코드.고객번호, 세션파일명, e1.Message, e1.StackTrace);
                            throw new Exception(메세지);
                        }
                    }

                    // ==============================================================================================================================================================================================
                    // ----------------------------------------------------------------------------------[ 여기까지가 기본출력 ] ------------------------------------------------------------------------------------
                    // ==============================================================================================================================================================================================

                    // ==================================
                    // 예외상품추가 코드
                    // ==================================

                    // ==================================
                    //      공통 별표 출력 (선택적) 
                    // ==================================
                    // Meritz의 독립특약의 공통별표와 다른 개념이다.
                    // 공통별표를 특약담보에 따라 선택적으로 출력한다.

                    // (추가담보상품인 경우)
                    string 현재세션코드 = "";

                    if (Tmp별표.Count > 0)
                    {
                        Tmp별표 = Tmp별표.Distinct().ToList();  // 중복제거
                        string 별표조건 = "'" + string.Join("','", Tmp별표) + "'";  // Array to String 변환 ex) '1234','4555','7777'

                        DataRow[] 개별별표 = ds02.Tables["타이틀tab"].Select(string.Format("구분 = '별표' and  담보 <> '' and  담보코드 in ({0})", 별표조건), "일련번호");

                        if (개별별표.Length == 0)
                        {

                            메세지 = string.Format("{0}|LER|{1}| 개별발표 검색실패(구분 = 별표 확인)", 인수, 레코드.고객번호);
                            throw new Exception(메세지);
                        }

                        // 별표기본타이틀 출력
                        // 처음 시작하는 표지가 맨처음에 나옴
                        // 세션당 기본타이틀은 1개만 존재한다.
                        현재세션코드 = "4";
                        검색쿼리 = string.Format("구분 like '%기본타이틀' and 세션 like '{0}%' and 담보 <>''", 현재세션코드);
                        기본타이틀 = ds02.Tables["타이틀tab"].Select(검색쿼리, "일련번호");  // 세션이 4로 시작하는  <============ 여기는 현재세션코드 주의 

                        if (기본타이틀.Length == 0)
                        {
                            메세지 = string.Format("{0}|LER|{1}| 기본타이틀 검색실패", 인수, 레코드.고객번호);
                            throw new Exception(메세지);
                        }

                        // ==================================
                        //        세션 기본타이틀 출력
                        // ==================================
                        // 삽입시 맨 앞으로 하면 문서의 속성이 그대로 이어진다 즉 스타일 바탕글이 새롭게 대체 된다. ( ※※※ 매우중요 ※※※ )

                        hwpbyte = 기본타이틀[0]["담보"].ToString();  // 세션, 기본타이틀 ex) 별표기본타이틀
                        await Hwp초기화(Ax02);
                        Ax02.MovePos(3, 0, 0);
                        Ax02.SetTextFile(hwpbyte, "HWP", "");  //※※※ 매우중요 ※※※ 
                        Ax02.MovePos(3, 0, 0);

                        row = 기본타이틀[0];
                        tmp형종권편장절 = string.Join("", row["형"], row["종"], row["권"], row["편"], row["장"], row["절"]);
                        tmp목차.Add(string.Join("|", row["일련번호"], row["담보명"], row["구분"], tmp형종권편장절));



                        int 별표갯수 = 개별별표.Length;
                        for (int i = 0; i < 별표갯수; i++)
                        {
                            if (string.IsNullOrWhiteSpace(개별별표[i]["담보"].ToString()))
                            {
                                메세지 = string.Format("{0}|LER|{1}| 별표출력오류", 인수, 레코드.고객번호);
                                throw new Exception(메세지);
                            }

                            hwpbyte = 개별별표[i]["담보"].ToString();
                            Ax02.MovePos(3, 0, 0);
                            Ax02.SetTextFile(hwpbyte, "HWP", "insertfile");

                            // ==================================
                            //   목차용 쪽번호 저장
                            // ==================================

                            총구역 = -1; 현재구역 = -1;
                            쪽 = -1; 단 = -1; 줄 = -1; 칸 = -1;
                            수정 = -1;
                            ctrlname = "";

                            // 목차에서 출력할 쪽번호를 저장한다.
                            Ax02.KeyIndicator(ref 총구역, ref 현재구역, ref 쪽, ref 단, ref 줄, ref 칸, ref 수정, ref ctrlname);
                            //목차쪽번호.Add(개별별표[i]["담보명"].ToString(), 쪽);
                            목차쪽번호.Add(new KeyValuePair<string, int>(개별별표[i]["담보명"].ToString(), 쪽));
                            Ax02.MovePos(3, 0, 0);

                            row = 개별별표[i];
                            tmp형종권편장절 = string.Join("", row["형"], row["종"], row["권"], row["편"], row["장"], row["절"]);
                            tmp목차.Add(string.Join("|", row["일련번호"], row["담보명"], row["구분"], tmp형종권편장절));
                        }

                        // ==================================
                        //    담보저장 ③/3
                        // ==================================

                        // 별표를 특별담보 처럼 출력한다.
                        try
                        {
                            별표Pcnt = Ax02.PageCount;
                            //PDF저장3
                            세션파일명 = string.Format("{0}{1}\\{2}_04_02.hwp", 레코드.출력폴더, 인수 + 1, 레코드.고객번호); //ex) 경로\고객번호_00_02.hwp  - 멀티처리를 위해 폴더를 분리해서 저장한다.
                            세션저장확인 = Ax02.SaveAs(세션파일명, "HWP", ""); //저장파일명Hwp
                            await Hwp초기화(Ax02);

                            if (세션저장확인 == true)
                            {
                                string 원본파일명 = 세션파일명;
                                string 디렉토리 = new FileInfo(원본파일명).DirectoryName;
                                string 원본이름 = new FileInfo(원본파일명).Name;

                                // 최초 대상 파일명: Tmp_aaa.hwp
                                string 대상파일명 = string.Format("{0}\\Tmp_{1}", 디렉토리, 원본이름);

                                int 최대재시도 = 3;
                                int 대기시간ms = 1000; // 이름이 바뀌므로 오래 대기할 필요 없이 1초면 충분합니다.

                                for (int i = 0; i < 최대재시도; i++)
                                {
                                    try
                                    {
                                        System.IO.File.Copy(원본파일명, 대상파일명, true);
                                        System.IO.File.Delete(원본파일명);
                                        break; // 복사 성공하면 루프 탈출
                                    }
                                    catch (System.IO.IOException) // 파일이 잠겨서 덮어쓰기 실패한 경우
                                    {
                                        if (i == 최대재시도 - 1)
                                        {
                                            throw new Exception($"파일 복사 최후 실패 (다른 프로세스 사용중): {대상파일명}");
                                        }

                                        // 실패 시 대상 파일명에 Tmp_를 한 번 더 붙여서 회피
                                        원본이름 = "Tmp_" + 원본이름;
                                        대상파일명 = string.Format("{0}\\Tmp_{1}", 디렉토리, 원본이름);
                                        // 1차 실패 후: Tmp_Tmp_aaa.hwp
                                        // 2차 실패 후: Tmp_Tmp_Tmp_aaa.hwp

                                        await Task.Delay(대기시간ms);
                                    }
                                    catch (UnauthorizedAccessException)
                                    {
                                        if (i == 최대재시도 - 1) throw new Exception($"파일 접근 권한 없음: {대상파일명}");
                                        await Task.Delay(대기시간ms);
                                    }
                                }
                            }

                        }
                        catch (Exception e1)
                        {
                            메세지 = string.Format("{0}|SER|{1}| 담보저장 ③/3 오류, 세션파일명-{2}, 오류-{3}==>{4} ", 인수, 레코드.고객번호, 세션파일명, e1.Message, e1.StackTrace);
                            throw new Exception(메세지);
                        }
                    }

                    // ==================================
                    //   목차출력
                    // ==================================

                    try
                    {

                        // 서식 적용 시 팝업창 무시하고 '예(Y)'를 누른 것과 같은 효과 부여
                        //Ax02.SetMessageBoxMode(0x00010000);

                        // 목차가 존재 하면 출력한다.
                        int 두번하기 = 0;
                        int 순수한목차cnt = -1;
                        int tmp보통약관Pcnt = 보통약관Pcnt;
                        int tmp특별약관Pcnt = 특별약관Pcnt;
                        int tmp제도성약관Pcnt = 제도성약관Pcnt;

                        //※※※ 매우중요 ※※
                        //두번처리하는 이유 : ###으로 인해 1줄로 목차를 표시해도 되는데 2줄로 표현이 되서 그게 누적되면 페이지가 상이하게 나옴
                        //서식을 적용하면 5페이지 목차가 4페이지로 줄어드는 현상 발생
                        //그로인해 목차 쪽번호가 다르게 표시됨
                        while (목차존재여부)
                        {
                            int 목차삽입페이지 = -1;
                            
                            string 임시목차경로 = string.Format("{0}목차\\{1}_IX_01.hwp", 레코드.출력폴더, 레코드.고객번호);
                            Ax02.Open(임시목차경로, "HWP", "");
                            Ax02.MovePos(3, 0, 0); // 문서의 마지막으로 이동

                            int 종료CtrlID = -1, 종료문단 = -1, 종료컬럼 = -1;
                            // 캐럿의 위치를 얻어온다.
                            Ax02.GetPos(ref 종료CtrlID, ref 종료문단, ref 종료컬럼);

                            Dictionary<string, int> 위치 = new Dictionary<string, int>();
                            int 저장확인 = 0;

                            총구역 = -1; 현재구역 = -1; 쪽 = -1; 단 = -1; 줄 = -1; 칸 = -1;
                            수정 = -1;
                            ctrlname = "";
                            int 현재CtrlID = -1, 현재문단 = -1, 현재컬럼 = -1;

                            Dictionary<string, int> 문단정보 = new Dictionary<string, int>();
                            List<string> 내용저장 = new List<string>();
                            int 위치시작 = -1;
                            int 위치종료 = 종료문단;

                            bool 별표출력예정 = false;
                            foreach (string item in tmp목차)
                            {
                                string[] 분리 = item.Split('|').Select(x => x.Trim()).ToArray();
                                if (분리.Length >= 3)
                                {
                                    // 목차 구분에 별표 관련 키워드가 하나라도 있는지 확인
                                    if (분리[2] == "별표기본타이틀" || 분리[2] == "공통별표" || 분리[2] == "별표" || 분리[2] == "독립별표")
                                    {
                                        별표출력예정 = true;
                                        break;
                                    }
                                }
                            }

                            for (int i = 0; i <= 종료문단; i++)
                            {
                                string 내용 = "";
                                bool 확인 = Ax02.InitScan(0x00, 0x0010, i, 0, i + 1, 0);
                                int 결과 = Ax02.GetText(ref 내용);
                                내용저장.Add(내용);

                                //강제로 위치를 변경하면 읽기를 못한다(?).
                                //위치를 옮기면 다시 원 위치를 해야 한다.
                                //본문내용이 변경되면 읽기를 못한다. (삽입이나 삭제가 있으면 뒤쪽부터 읽으면 된다.) 아래쪽 목차수정부분에 있음
                                Ax02.MovePos(1, i, 0);  // 특정위치로, i문단의번호로
                                Ax02.KeyIndicator(ref 총구역, ref 현재구역, ref 쪽, ref 단, ref 줄, ref 칸, ref 수정, ref ctrlname);
                                Ax02.MovePos(3, 0, 0); // 1:특정위치, 2: 문서의시작, 3:문서의 끝

                                string tmp문자열 = "";
                                // 정상시작(마지막 라인부터 읽기 시작한다.)
                                if (내용.Contains("***위치정보시작***"))
                                {
                                    저장확인 = 1;
                                    tmp문자열 = "위치정보시작";

                                    위치시작 = i;

                                    // 👇 위에서 구한 '별표출력예정' 변수를 여기서 사용합니다. 👇
                                    if (별표출력예정 == false)
                                    {
                                        for (int p = i - 3; p <= i - 1; p++)
                                        {
                                            if (내용저장[p].Trim() == "")
                                            {
                                                위치시작 = p;
                                                break;
                                            }
                                        }
                                    }
                                }
                                else if (내용.Contains("***위치정보종료***"))
                                {
                                    저장확인 = 2;
                                    tmp문자열 = "위치정보종료";
                                }
                                else
                                {
                                    tmp문자열 = string.Format("{0}-{1}-{2}", 쪽, 단, 줄);
                                    // 한글파일에 값과 비교하기 위해  현재구역(문단)값을 저장한다. 
                                    // 한글은 위치값으로 문단값만 사용한다. 쪽-단-줄 값으로 문단값을 찾는다.
                                }

                                문단정보.Add(tmp문자열, i);  // ex) 23-2-10,  25(문단)

                                // 쪽-단-줄 위치정보를 문단 위치정보로 교환한다.
                                // ***위치정보종료*** 값이 나올때가 까지 계속 저장확인수는 1 이다.

                                if (저장확인 == 1)
                                {
                                    string[] 분리 = 내용.Trim().Split(':').Select(P => P.Trim().Replace("\r", "").Replace("\n", "")).ToArray(); ;
                                    if (분리.Length == 2)
                                    {
                                        int tmpValue = 0;
                                        // 출력위치 - 특별담보, 별표, 독립특약 등....
                                        // 쪽,단,줄을 키값으로 사용
                                        if (문단정보.ContainsKey(분리[1]))
                                        {
                                            tmpValue = 문단정보[분리[1]];
                                        }
                                        else if (분리[0] == "목차삽입페이지" ||
                                                    분리[0] == "목차전체페이지" ||
                                                    분리[0].Substring(0, 5) == "적용서식_")
                                        {
                                            // 목차삽입페이지 : 삽입할 페이지 위치
                                            // 적용서식 : 스타일 인덱스
                                            tmpValue = Convert.ToInt32(분리[1]);
                                        }
                                        else
                                        {
                                            tmpValue = -1;  // 값이 없는 경우 -1 값으로 넣어 두었음
                                        }
                                        위치.Add(분리[0], tmpValue);
                                    }
                                }
                            } // END for

                            if (위치.TryGetValue("목차삽입페이지", out 목차삽입페이지))
                            {

                            }
                           

                            if (저장확인 != 2)
                            {
                                메세지 = string.Format("{0}|LER|{1}| 목차생성규칙오류, 위치정보(시작,종료)검색실패", 인수, 레코드.고객번호);
                                throw new Exception(메세지);
                            }
                            else
                            {
                                // 위치정보 삭제
                                ////Ax02.MovePos(1, 문단정보["위치정보종료"], 0);            // 현재위치로 이동
                                ////Ax02.MovePos(7, 0, 0);                                   // 현재 위치한 문단의 끝으로 이동
                                ////Ax02.GetPos(ref 현재CtrlID, ref 현재문단, ref 현재컬럼);
                                ////bool 설정1 = Ax02.SelectText(문단정보["위치정보시작"], 0, 문단정보["위치정보종료"], 현재컬럼);  // 서식을 적용 할 문단을 블럭잡음

                                Ax02.MovePos(1, 위치종료, 0);                                 // 현재위치로 이동
                                Ax02.MovePos(7, 0, 0);                                        // 현재 위치한 문단의 끝으로 이동
                                Ax02.GetPos(ref 현재CtrlID, ref 현재문단, ref 현재컬럼);
                                bool 설정1 = Ax02.SelectText(위치시작, 0, 위치종료, 현재컬럼);// 서식을 적용 할 문단을 블럭잡음

                                Ax02.Run("Delete"); Ax02.Run("Delete");  Ax02.Run("Delete");
                            }
                            

                            if (위치.ContainsKey("출력위치_특별담보") == false)
                            {
                                메세지 = string.Format("{0}|LER|{1}| 목차파일에 '출력위치_특별담보' 지정 키워드 오류, 출력위치정보가 없음", 인수, 레코드.고객번호);
                                throw new Exception(메세지); ;
                            }

                            // ==================================
                            //   커서이동 ①
                            // ==================================

                            Ax02.MovePos(1, 위치["출력위치_특별담보"], 0);  // 특별담보목차 위치로 이동한다.

                            //목차분리[0] : 일련번호
                            //목차분리[1] : 담보명
                            //목차분리[2] : 구분
                            //목차분리[3] : 형종권편장절
                            //일련번호, 담보명, 구분, 형종권편장절

                            int 삽입문단cnt = 0;
                            // 특별약관 다음에 출력 되는 '별표'를 위해 사용
                            // 원래 값보다 별표출력 위치가 증가한다.(특별약관 관련 타이틀, 담보제목이 출력 되기 때문) 

                            bool 독립담보여부 = false;
                            int 목차인수 = -1;
                            for (int r = 0; r < tmp목차.Count; r++)
                            {
                                string[] 목차분리 = tmp목차[r].Split('|').Select(x => x.Trim()).ToArray(); ;
                                // 출력제외
                                if (목차분리[2] == "특별약관기본타이틀")
                                {
                                    continue;
                                }
                                else if (목차분리[2] == "공통별표")
                                {

                                    // 공통별표가 나오면 다음부터는 독립담보 및 별표가 나오는 걸로 간주 한다.
                                    if (위치.ContainsKey("출력위치_독립담보"))
                                    {
                                        // ==================================
                                        //   커서이동 ③
                                        // ==================================
                                        Ax02.MovePos(1, 위치["출력위치_독립담보"] + 삽입문단cnt, 0);
                                    }
                                    독립담보여부 = true;
                                    continue;
                                }
                                else if (목차분리[2] == "별표기본타이틀")
                                {

                                    // 별표출력값이 없으면 특별담보 내용 다음에 계속 이어 출력 한다.
                                    if (위치.ContainsKey("출력위치_별표"))
                                    {

                                        // ==================================
                                        //   커서이동 ②
                                        // ==================================
                                        Ax02.MovePos(1, 위치["출력위치_별표"] + 삽입문단cnt, 0);
                                    }
                                    
                                }

                                // 특별담보타이틀
                                string 서식구분명 = "";
                                if (목차분리[2].Contains("담보타이틀"))
                                {
                                    // 뒤부터 00이 아닌 값을 찾아 해당 '형종권편장절'을 적용한다.
                                    // 000000000200
                                    for (int ii = 10; ii >= 0; ii -= 2)
                                    {
                                        string 서식구분 = 목차분리[3].Substring(ii, 2);
                                        if (서식구분 != "00")
                                        {
                                            switch (ii)
                                            {
                                                case 10: 서식구분명 = "적용서식_절"; break;
                                                case 8: 서식구분명 = "적용서식_장"; break;
                                                case 6: 서식구분명 = "적용서식_편"; break;
                                                case 4: 서식구분명 = "적용서식_권"; break;
                                                case 2: 서식구분명 = "적용서식_종"; break;
                                                case 0: 서식구분명 = "적용서식_형"; break;
                                            }
                                            break;
                                        }
                                    }
                                }
                                else if (목차분리[2] == "특별담보" ||
                                            목차분리[2] == "보통담보" ||
                                            목차분리[2] == "별표" ||
                                            목차분리[2] == "독립별표" ||
                                            목차분리[2] == "특별담보그룹타이틀"
                                            )
                                {

                                    서식구분명 = "적용서식_담보";

                                    if (독립담보여부 == true && 목차분리[2] == "특별담보")
                                    {
                                        서식구분명 = "적용서식_타이틀";
                                    }

                                }
                                else if (목차분리[2] == "별표기본타이틀")
                                {
                                    서식구분명 = "적용서식_장";
                                }

                                if (위치.ContainsKey(서식구분명) == false)
                                {
                                    메세지 = string.Format("{0}|LER|{1}| 목차파일에 '{2}' 지정 키워드 오류, 출력위치정보가 없음", 인수, 레코드.고객번호, 서식구분명);
                                    throw new Exception(메세지);
                                }

                                if (목차분리[2].Contains("담보타이틀") || 목차분리[2] == "별표기본타이틀")
                                {
                                    Ax02.Run("BreakPara");   // Enter
                                    삽입문단cnt++;
                                }

                                // 순서 ) 보통약관 - 특별약관 ( 선택특약 => 공통별표 => 독립특약 ) - 제도성약관 
                                // 즉 : 선택특약, 공통별표, 독립특약 모두 특별약관 처럼 처리한다.
                                // 
                                string 목차출력 = 목차분리[1];
                                bool 출력여부 = true;
                                string 구분문구 = "";

                                //소문자로 출력하는건 프로그램에의해 출력하는(소문자)문자와 편집에서 하드출력하는(대문자)를 구분하기 위함이다.
                                //아래쪽에 목차서식(탭 ......... 쪽번호)을 넣기 위함이다.
                                //주의요망
                                switch (목차분리[2])
                                {
                                    case "보통담보":
                                        구분문구 = "h"; break;
                                    case "특별담보":
                                    case "공통별표":
                                    case "독립별표":
                                        구분문구 = "b"; break;
                                    case "별표":
                                        구분문구 = "s"; break;
                                    default:
                                        출력여부 = false; break;
                                }
                                //제도성약관 : T

                                if (출력여부)
                                {
                                    // 1. LIKE (포함) 방식으로 검색
                                    var 매칭항목 = 목차쪽번호.FirstOrDefault(x => x.Key != null && (x.Key.Contains(목차출력) || 목차출력.Contains(x.Key)));

                                    if (매칭항목.Key != null)
                                    {
                                        // 찾았을 경우 처리
                                        목차출력 = string.Format("{0}###{1}{2}", 목차출력, 구분문구, 매칭항목.Value);

                                        // 인덱스 동기화 (순서 꼬임 방지)
                                        int 찾은인덱스 = 목차쪽번호.IndexOf(매칭항목);
                                        if (찾은인덱스 >= 목차인수) 목차인수 = 찾은인덱스 + 1;
                                    }
                                    else
                                    {
                                        // [수정됨] 목차인수가 -1일 경우 0으로 보정하여 배열 참조 오류 방지
                                        int 안전한목차인수 = Math.Max(0, 목차인수);

                                        // 2. LIKE 검색 실패 시: 샌드위치(n+1 ~ n+3) 기법 적용
                                        bool 샌드위치성공 = false;
                                        int 최대미리보기 = 3; // n+1, n+2, n+3 까지 검사
                                        int 유효항목카운트 = 0; // 뒤따라오는 유효한(출력여부==true) 항목의 개수를 셈

                                        // tmp목차에서 현재(r) 이후의 항목들을 탐색
                                        for (int nextR = r + 1; nextR < tmp목차.Count; nextR++)
                                        {
                                            string[] next분리 = tmp목차[nextR].Split('|').Select(x => x.Trim()).ToArray(); ;
                                            string next구분 = next분리[2];

                                            // 출력이 유효한 항목(출력여부 == true 가 될 항목)인지 확인
                                            if (next구분 == "보통담보" || next구분 == "특별담보" || next구분 == "공통별표" || next구분 == "독립별표" || next구분 == "별표")
                                            {
                                                유효항목카운트++;
                                                string 다음목차출력 = next분리[1];

                                                // [수정됨] 비교할 DB인덱스 산출 시 보정된 '안전한목차인수' 사용
                                                int 타겟DB인덱스 = 안전한목차인수 + 유효항목카운트;

                                                // DB 리스트 범위를 벗어나지 않는지 확인
                                                if (타겟DB인덱스 < 목차쪽번호.Count)
                                                {
                                                    string 다음DB키 = 목차쪽번호[타겟DB인덱스].Key;

                                                    // n+x 항목이 서로 매칭(Like)된다면, 현재(n)는 확정적으로 일치한다고 간주
                                                    if (다음DB키 != null && (다음DB키.Contains(다음목차출력) || 다음목차출력.Contains(다음DB키)))
                                                    {
                                                        샌드위치성공 = true;
                                                        break; // 하나라도 일치하면 성공이므로 반복문 즉시 탈출
                                                    }
                                                }

                                                // 최대 설정한 개수(n+3)까지만 검사하고 종료
                                                if (유효항목카운트 >= 최대미리보기)
                                                {
                                                    break;
                                                }
                                            }
                                        }

                                        if (샌드위치성공)
                                        {
                                            // [샌드위치 검증 통과] 보정된 인덱스로 접근
                                            int 목차val = 목차쪽번호[안전한목차인수].Value;
                                            목차출력 = string.Format("{0}###{1}{2}", 목차출력, 구분문구, 목차val);

                                            // [수정됨] 다음 처리를 위해 목차인수 실제 갱신
                                            목차인수 = 안전한목차인수 + 1;
                                        }
                                        else
                                        {
                                            // 3. 샌드위치(n+3까지)도 안 될 경우의 최후 보루 (유사도 검사)
                                            // [수정됨] 보정된 인덱스로 안전하게 한 번 더 범위 체크
                                            if (안전한목차인수 < 목차쪽번호.Count)
                                            {
                                                var 임시목차 = 목차쪽번호[안전한목차인수];
                                                string 목차key = 임시목차.Key;
                                                int 목차val = 임시목차.Value;

                                                double similarity = 정확도계산(목차출력, 목차key);
                                                double threshold = 0.5;

                                                if (similarity >= threshold)
                                                {
                                                    목차출력 = string.Format("{0}###{1}{2}", 목차출력, 구분문구, 목차val);

                                                    // [수정됨] 다음 처리를 위해 목차인수 실제 갱신
                                                    목차인수 = 안전한목차인수 + 1;
                                                }
                                                else
                                                {
                                                    목차출력 = string.Format("{0}###{1}{2}", 목차출력, 구분문구, "");
                                                }
                                            }
                                            else
                                            {
                                                목차출력 = string.Format("{0}###{1}{2}", 목차출력, 구분문구, "");
                                            }
                                        }
                                    }
                                }

                                HwpAction hwpact;
                                HwpParameterSet textWrite;
                                hwpact = (HwpAction)Ax02.CreateAction("InsertText"); // 텍스트 추가
                                textWrite = (HwpParameterSet)hwpact.CreateSet();
                                textWrite.SetItem("Text", 목차출력);  // 목차출력
                                hwpact.Execute(textWrite);


                                // ※※※※※ 매우중요 ※※※※※
                                // 스타일 적용 반드시 적용되는 라인은 바탕글로 되어 있어야 한다.
                                // 만약 다른 스타일로 적용되어 있으면 적용여부를 묻는 창이 뜬다.
                                // 아래 코딩은 강제로 바탕글로 적용(스타일을 모두 기본으로 바꿈)
                                현재CtrlID = -1; 현재문단 = -1; 현재컬럼 = -1;
                                Ax02.GetPos(ref 현재CtrlID, ref 현재문단, ref 현재컬럼);
                                Ax02.SelectText(현재문단, 0, 현재문단, 현재컬럼);
                                Ax02.Run("CharShapeNormal");
                                Ax02.Run("ParaShapeNormal");

                                HwpAction hwpact01;
                                HwpParameterSet textStyle;
                                hwpact01 = (HwpAction)Ax02.CreateAction("Style"); // 스타일 적용
                                textStyle = (HwpParameterSet)hwpact01.CreateSet();

                                textStyle.SetItem("Apply", 위치[서식구분명]);     // 서식인덱스
                                hwpact01.Execute(textStyle);

                                Ax02.MovePos(7, 0, 0);   // 현재 위치한 문단의 끝으로 이동
                                Ax02.Run("BreakPara");   // Enter

                                textStyle.SetItem("Apply", 0); // 기본스타일(바탕글)로 변경
                                hwpact01.Execute(textStyle);

                                삽입문단cnt++;
                            }

                            //Ax02.Save();
                            // 목차페이지 리뉴얼
                            Ax02.MovePos(3, 0, 0);
                            종료CtrlID = -1; 종료문단 = -1; 종료컬럼 = -1;
                            // 캐럿의 위치를 얻어온다.
                            Ax02.GetPos(ref 종료CtrlID, ref 종료문단, ref 종료컬럼);

                            // ###때문에 전체페이지가 미세하게 오류가 발생한다.
                            // 매우중요
                            // 두번하기변수가 0이아니면 전체페이지가 불일치하여 다시 하는거임
                            순수한목차cnt = (두번하기 == 0) ? Ax02.PageCount : 순수한목차cnt;
                            int 목차Pcnt = 순수한목차cnt;
                            string 찾는문자열 = "###";

                            // 우편이면 짝홀수페이지를 설정한다.
                            if (레코드.발송구분 == "01" || 레코드.발송구분 == "1")
                            {
                                if (목차Pcnt % 2 == 1)
                                    목차Pcnt++;

                                // 목차삽입페이지가 짝수이면 "목차"세션을 맞추기 위해 빈페이지를 삽입한다.
                                // 세션시작 페이지는 반드시 홀수페이지 이어만 한다.
                                // 목차페이지 삽입전에 보통약관에 빈페이지를 출력한다.
                                if (목차삽입페이지 % 2 == 0)
                                    보통약관Pcnt++;

                                보통약관Pcnt = 목차Pcnt + 보통약관Pcnt;

                                if (보통약관Pcnt % 2 == 1)
                                    보통약관Pcnt++;

                                if (특별약관Pcnt % 2 == 1)
                                    특별약관Pcnt++;

                                if (제도성약관Pcnt % 2 == 1)
                                    제도성약관Pcnt++;
                            }
                            else
                            {
                                보통약관Pcnt = 목차Pcnt + 보통약관Pcnt;
                            }

                            //강제로 위치를 변경하면 읽기를 못한다(?).
                            //위치를 옮기면 다시 원 위치를 해야 한다.
                            //본문내용이 변경되면 읽기를 못한다. (삽입이나 삭제가 있으면 뒤쪽부터 읽으면 된다.) 아래쪽 목차수정부분에 있음
                            for (int i = 종료문단; i >= 0; i--)
                            {
                                string 내용 = "";
                                bool 확인 = Ax02.InitScan(0x00, 0x0010, i, 0, i + 1, 0);
                                int 결과 = Ax02.GetText(ref 내용);
                                if (내용.Contains(찾는문자열))
                                {
                                    Ax02.MovePos(1, i, 0);                                   // 현재위치로 이동
                                    Ax02.MovePos(7, 0, 0);                                   // 현재 위치한 문단의 끝으로 이동
                                    Ax02.GetPos(ref 현재CtrlID, ref 현재문단, ref 현재컬럼);
                                    bool 설정1 = Ax02.SelectText(i, 0, i, 현재컬럼);         // 서식을 적용 할 문단을 블럭잡음
                                    Ax02.Run("Delete");

                                    int 변경위치 = 내용.IndexOf(찾는문자열);  // ###위치를 찾는다.
                                    int 현재쪽수 = -1;

                                    //예시) 프로그램으로 출력하면 : ..........###B123 => 가변적인 출력이면 H(보통담보),B(특별담보,공통별포,독립별표),S(별표) 
                                    //      하드출력이면          : ..........###123  => 편집팀에서 할때

                                    string 세션문자열 = string.Empty;
                                    int 숫자 = -1;

                                    if (string.IsNullOrWhiteSpace(내용.Substring(변경위치 + 3)))
                                    {
                                        // 목차를 살펴 볼것 쪽수가 없음
                                        메세지 = string.Format("{0}|SER|{1}| 타이틀_MB.hwp 파일에 목차리스트에 쪽수가 없음, 오류-{2}==> 쪽수입력 요망", 인수, 레코드.고객번호, 찾는문자열);
                                        throw new Exception(메세지);
                                    }
                                    else
                                    {
                                        // 숫자여부 확인
                                        세션문자열 = 내용.Substring(변경위치 + 3, 1);
                                        if (int.TryParse(세션문자열, out 숫자))
                                        {
                                            현재쪽수 = Convert.ToInt32(내용.Substring(변경위치 + 3));
                                            세션문자열 = "H";
                                        }
                                        else
                                        {
                                            현재쪽수 = Convert.ToInt32(내용.Substring(변경위치 + 4));
                                        }
                                    }

                                    // 보통, 특별, 제도, 발표, 독립 ...
                                    int 계산위치쪽수 = -1;
                                    switch (Convert.ToString(세션문자열).ToUpper())
                                    {
                                        case "S":  // 별표
                                            계산위치쪽수 = 현재쪽수 + 제도성약관Pcnt + 특별약관Pcnt + 보통약관Pcnt;
                                            break;
                                        case "T":  // 제도성약관  
                                            계산위치쪽수 = 현재쪽수 + 특별약관Pcnt + 보통약관Pcnt;
                                            break;
                                        case "B":  // 특별약관 ( 선택특약 => 공통별표 => 독립특약 )                                                                                        
                                            계산위치쪽수 = 현재쪽수 + 보통약관Pcnt;
                                            break;
                                        case "H":  // 보통약관 (목차포함)
                                            if (현재쪽수 >= 목차삽입페이지)
                                            {
                                                계산위치쪽수 = 현재쪽수 + 목차Pcnt;
                                            }
                                            else
                                            {
                                                계산위치쪽수 = 현재쪽수; // 목차페이지 삽입 이전 페이지로 페이지 변화가 없다.
                                            }
                                            break;
                                        default:
                                            break;
                                    }

                                    string 변경내용 = 내용.Substring(0, 변경위치);
                                   
                                    if (레코드.고객사 != "MG")
                                    {
                                        switch (Convert.ToString(세션문자열))
                                        {
                                            case "s":
                                            case "t":
                                            case "b":
                                            case "h":
                                                변경내용 = 변경내용.Replace("\n", "").Replace("\r", "") + '\t' + (계산위치쪽수);
                                                break;
                                            default:
                                                변경내용 = 변경내용.Replace("\n", "").Replace("\r", "") + (계산위치쪽수);
                                                break;
                                        }
                                    }

                                    HwpAction hwpact;
                                    HwpParameterSet textWrite;
                                    hwpact = (HwpAction)Ax02.CreateAction("InsertText"); // 텍스트 추가
                                    textWrite = (HwpParameterSet)hwpact.CreateSet();
                                    textWrite.SetItem("Text", 변경내용);  // 목차출력
                                    hwpact.Execute(textWrite);
                                }
                            }
                            if (순수한목차cnt != Ax02.PageCount)
                            {
                                순수한목차cnt = Ax02.PageCount;
                                Ax02.Clear(1);
                                두번하기++;
                                보통약관Pcnt = tmp보통약관Pcnt;
                                특별약관Pcnt = tmp특별약관Pcnt;
                                제도성약관Pcnt = tmp제도성약관Pcnt;

                                if (두번하기 < 2)
                                    continue;
                            }

                            Ax02.Save();
                            await Hwp초기화(Ax02);
                            string 목차저장경로 = string.Format("{0}\\{1}\\Tmp_{2}_IX_{3:000}.hwp", 레코드.출력폴더, 인수 + 1, 레코드.고객번호, 목차삽입페이지); //ex) 경로\고객번호_IX_삽입페이지(3자리).hwp  - 삽입페이지 : 파일 결합시 H파일에 목차를 삽입한다.
                            new FileInfo(임시목차경로).CopyTo(목차저장경로, true);
                            break;
                            
                        }
                    }
                    catch (Exception ee)
                    {
                        메세지 = string.Format("{0}|SER|{1}| 알수없는오류, 오류 - {2}==>{3}", 인수, 레코드.고객번호, ee.Message, ee.StackTrace);
                        throw new Exception(메세지);
                    }

                    // ----------------------------------------------------------------------------------[ PDF 출력 기본_H, 기본_B, 기본_T... ] ------------------------------------------------------------------------------------


                    // ======================================================
                    //   보통약관 + 특별약관 + 제도성약관 + 별표 + 색인 결합
                    // ======================================================
                    // 보통약관(헤더), 제도성약관(테일)
                    // 기본_H, 기본_T, 기본_I등  모두 결합하여 선택약관을 완성한다. 위에서 만든 가변 약관은은 로컬이 있으니 해당세션만 서로 맞추어 결합 후 다시 전체를 결합한다.
                }
                메세지 = string.Format("{0}|SCU|{1}| 개별약관생성완료(담보갯수[{2:000}])완료", 인수, 레코드.고객번호, 담보갯수);
            }
            catch (OutOfMemoryException m)
            {
                메세지 = string.Format("{0}|MER|{1}| OutOfMemoryException, 오류-{2}==>{3}", 인수, 레코드.고객번호, m.Message, m.StackTrace);
            }
            catch (Exception e)
            {
                int count = 메세지.Count(c => c == '|');
                if (count < 3) 
                    메세지 = string.Format("{0}|SER|{1}| 알수없는오류, 오류-{2}==>{3}", 인수, 레코드.고객번호, e.Message, e.StackTrace);
            }
            return 메세지;
        }

        // 두 문자열의 유사도를 계산하는 함수 (0.0 ~ 1.0 반환)
        public double 정확도계산(string source, string target)
        {
            if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(target)) return 0.0;
            if (source == target) return 1.0;

            int stepsToSame = ComputeLevenshteinDistance(source, target);
            return (1.0 - ((double)stepsToSame / (double)Math.Max(source.Length, target.Length)));
        }

        // 레벤슈타인 거리 알고리즘 (편집 거리 계산)
        private int ComputeLevenshteinDistance(string source, string target)
        {
            int n = source.Length;
            int m = target.Length;
            int[,] d = new int[n + 1, m + 1];

            if (n == 0) return m;
            if (m == 0) return n;

            for (int i = 0; i <= n; d[i, 0] = i++) { }
            for (int j = 0; j <= m; d[0, j] = j++) { }

            for (int i = 1; i <= n; i++)
            {
                for (int j = 1; j <= m; j++)
                {
                    int cost = (target[j - 1] == source[i - 1]) ? 0 : 1;
                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost);
                }
            }
            return d[n, m];
        }

        // ==========================================================================================================
        // 03 - 04 : 컨트롤러앱실행
        // ==========================================================================================================
        private async Task<string> 컨트롤러앱실행(현재상태 상태, string 앱제어)
        {
            단계진행문구출력("00");
            string 실행여부 = "성공";
            const int 최대재시도 = 3;

            string 실행파일명 = "H2PControl.exe";
            string 실행경로 = @"c:\Easy약관Plus\program\H2P\H2PControl\";
            //string 실행경로 = @"d:\Easy약관Plus\Easy약관PDF\H2PControl\bin\Debug\";

            string 실행파일전체경로 = System.IO.Path.Combine(실행경로, 실행파일명);
            string 프로세스이름 = System.IO.Path.GetFileNameWithoutExtension(실행파일명);

            try
            {

                // 🔹 앱실행여부  확인 (정확히 같은 경로의 프로세스만)
                bool 앱실행여부 = FindOrKillProcessByPath(프로세스이름, 실행파일전체경로, "check");

                switch (앱제어)
                {
                    case "앱실행_싱글처리":
                    case "앱실행_재시작":
                        if (앱실행여부)
                        {
                            로그내용 = $"{DateTime.Now:yy.MM.dd HH:mm:ss} [H2P APP종료] H2PControl APP 종료 : {앱제어} H2P APP을 종료합니다.";
                            로그기록(로그내용, 1);
                            FindOrKillProcessByPath(프로세스이름, 실행파일전체경로, "kill");

                            // 약간의 대기 후 재실행 (완전 종료 보장)
                            await Task.Delay(TimeSpan.FromSeconds(5));
                        }
                        break;
                    case "앱실행_일반":
                        if (앱실행여부)
                        {
                            // 🔸 이미 실행 중이면 패스
                            로그내용 = $"{DateTime.Now:yy.MM.dd HH:mm:ss} [H2P APP실행] H2PControl APP 실행중 : 일반처리시 H2P APP이 실행 중이면 PASS";
                            로그기록(로그내용, 1);
                            return 실행여부;
                        }
                        break;
                }

                // 🔸 실행되지 않았으면 새로 실행
                for (int 시도 = 1; 시도 <= 최대재시도; 시도++)
                {
                    try
                    {
                        ProcessStartInfo startInfo = new ProcessStartInfo
                        {
                            FileName = 실행파일전체경로,
                            WorkingDirectory = 실행경로,
                            //Arguments = $"\"{상태.groupKey}\"", // 따옴표로 감싸기
                            Arguments = 앱제어,
                            UseShellExecute = true                 // OS 셸을 사용하여 실행 (탐색기에서 실행한 것과 동일한 효과)
                        };

                        H2PControl제어 = new Process { StartInfo = startInfo };
                        H2PControl제어.Start();

                        try
                        {
                            H2PControl제어.WaitForInputIdle(1000);
                        }
                        catch
                        {
                            // 창이 없는 콘솔 앱 등일 경우 발생하는 예외 무시
                        }
                        Process[] processes = Process.GetProcessesByName(프로세스이름);
                        foreach (Process p in processes)
                        {
                            if (p.MainWindowHandle != IntPtr.Zero)
                            {
                                ShowWindow(p.MainWindowHandle, SW_RESTORE);
                                SetForegroundWindow(p.MainWindowHandle);
                            }
                        }


                        로그내용 = $"{DateTime.Now:yy.MM.dd HH:mm:ss} [H2P APP실행] H2PControl APP 실행 : {앱제어} H2P APP을 시작 합니다.";
                        로그기록(로그내용, 1);
                        break;
                    }
                    catch (Exception ex)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(5));
                        실행여부 = "실패";

                        if (시도 == 최대재시도)
                        {
                            로그내용 = $"{DateTime.Now:yy.MM.dd HH:mm:ss} [ERROR] H2P APP실행 오류 : {시도}번 모두 실행 실패 - {ex.Message}";
                            로그기록(로그내용, 1);
                            throw;
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                실행여부 = "실패";
                로그내용 = $"{DateTime.Now:yy.MM.dd HH:mm:ss} [ERROR] ◎진행불가 : H2P APP실행 오류 - {ex.Message}";
                로그기록(로그내용, 1);
            }


            return 실행여부;
        }

        // ==========================================================================================================
        // 03 - 05 : 컨트롤러앱검사 및 종료
        // ==========================================================================================================

        private bool FindOrKillProcessByPath(string processName, string exePath, string mode)
        {
            bool 찾기 = false;
            string procNameOnly = System.IO.Path.GetFileNameWithoutExtension(processName);

            try
            {
                Process[] processes = Process.GetProcessesByName(procNameOnly);

                foreach (Process p in processes)
                {
                    // ★ 중요: MainModule을 호출하지 않고 바로 API를 씁니다. (오류 원천 차단)
                    string currentPath = GetProcessPathByApi(p.Id);

                    if (!string.IsNullOrEmpty(currentPath) &&
                        string.Equals(currentPath, exePath, StringComparison.OrdinalIgnoreCase))
                    {
                        찾기 = true;

                        if (mode == "kill")
                        {
                            try
                            {
                                p.Kill();
                                p.WaitForExit(1000);
                                로그내용 = $"{DateTime.Now:yy.MM.dd HH:mm:ss} [앱종료] {processName} (PID: {p.Id})";
                                로그기록(로그내용, 1);
                            }
                            catch (Exception ex)
                            {
                                // 권한 문제로 종료 못 할 수도 있음
                                로그내용 = $"{DateTime.Now:yy.MM.dd HH:mm:ss} [종료실패] {ex.Message}";
                                로그기록(로그내용, 1);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                로그내용 = $"{DateTime.Now:yy.MM.dd HH:mm:ss} [프로세스조회오류] {ex.Message}";
                로그기록(로그내용, 1);
            }

            return 찾기;
        }
        // 경로 가져오기 헬퍼 함수

        private string GetProcessPathByApi(int pid)
        {
            IntPtr hProcess = IntPtr.Zero;
            try
            {
                // 제한된 권한으로 열기 때문에 32비트 앱에서도 64비트 앱 정보를 볼 수 있음
                hProcess = OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, false, pid);

                // 접근 권한이 아예 없는 시스템 프로세스(백신, 윈도우 커널 등)는 0을 반환함
                if (hProcess == IntPtr.Zero) return "";

                int capacity = 1024;
                StringBuilder sb = new StringBuilder(capacity);
                int size = capacity;

                if (QueryFullProcessImageName(hProcess, 0, sb, ref size))
                {
                    return sb.ToString();
                }
            }
            catch
            {
                // 혹시 모를 API 오류 무시
            }
            finally
            {
                if (hProcess != IntPtr.Zero) CloseHandle(hProcess);
            }
            return "";
        }

        // ==========================================================================================================
        // 04 H2PControl제어
        //       F1 ~ F8 : Hwp to PDF 변환 후 개별약관별로 PDF병합  
        // ==========================================================================================================
        private async Task<string> 컨트롤러앱완료대기(현재상태 상태)
        {
            string 실행여부 = "성공";
            int 재시도 = 0;
            int 체크 = 0;
            DirectoryInfo di = new DirectoryInfo(상태.출력폴더);
            // EnumerateFiles와 LINQ를 사용하여 모든 파일의 크기를 합산
            // SearchOption.AllDirectories는 모든 하위 폴더까지 탐색하도록 지시
            long 처음용량 = di.EnumerateFiles("*", SearchOption.AllDirectories).Sum(fi => fi.Length);

            string 감시폴더 = $"{저장.기본출력폴더}감시\\";

            while (true)
            {
                try
                {
                    체크++;
                    string 완료파일 = $"{감시폴더}{상태.groupKey}.단계완료";
                    if (new FileInfo(완료파일).Exists) {                       
                        return 실행여부;
                    }
                    else
                    {
                        await Task.Delay(TimeSpan.FromSeconds(3));
                        if (체크 % 100 == 0) // 5분 동안 용량변화가 없으면 에러로 간주한다.
                        {
                            long 다음용량 = di.EnumerateFiles("*", SearchOption.AllDirectories).Sum(fi => fi.Length);
                            if (다음용량 > 처음용량)
                            {
                                string formattedSize = FormatBytes(다음용량);

                                로그내용 = string.Format("{0:yy.MM.dd HH:mm:ss} [약관생성진행]현재 약관생성/변환 진행 중 {1} ", DateTime.Now, formattedSize);
                                로그기록(로그내용, 1);
                                처음용량 = 다음용량;
                                continue;
                            } else  {
                                //H2PControl 재시작
                                _ = 컨트롤러앱실행(상태, "앱실행_재시작");
                            }
                        }
                    }
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
                    throw;
                }
            }
        }

        // 바이트를 KB, MB, GB 등으로 변환하는 헬퍼 함수
        public static string FormatBytes(long bytes)
        {
            string[] suffix = { "B", "KB", "MB", "GB", "TB" };
            int i = 0;
            double dblSByte = bytes;

            while (dblSByte >= 1024 && i < suffix.Length - 1)
            {
                dblSByte /= 1024;
                i++;
            }

            return string.Format("{0:0.##} {1}", dblSByte, suffix[i]);
        }
       
        // ==========================================================================================================
        // 04 - 02 앱이 완료/종료되면 DB에 상태값을 Update 한다.
        // ==========================================================================================================
        private async Task<string> 병렬처리DBUpdate(현재상태 상태, string 개별, dynamic 집계결과 )
        {
            string 실행여부 = "성공";
            int 재시도 = 0;

            int 우편cnt = 0;
            int 모바일cnt = 0;
            int 이메일cnt = 0;
            if (집계결과 != null) {
                우편cnt = 집계결과.우편cnt;
                모바일cnt = 집계결과.모바일cnt;
                이메일cnt = 집계결과.이메일cnt;
            }
            while (true)
            {
                try
                {
                    int 최대인수 = 7; //약관작업, PDF변환작업, "PDF병합작업, 개별PDF작업, 우편작업, FTP작업, 모바일작업
                    int[] 작업건수 = new int[최대인수 + 1];
                    using (var reader = await LoadReader(상태, 개별).ConfigureAwait(false))
                    {
                        while (await reader.ReadAsync().ConfigureAwait(false))
                        {
                            작업건수[0] = Convert.ToInt16(reader["약관작업cnt"].ToString());
                            작업건수[1] = Convert.ToInt16(reader["PDF변환작업cnt"].ToString());
                            작업건수[2] = Convert.ToInt16(reader["PDF병합작업cnt"].ToString()); // (필수)여기까지는 main,sub1 ~ sub3모두 해당

                            작업건수[3] = Convert.ToInt16(reader["개별PDF작업cnt"].ToString()); // (필수)main컴만

                            작업건수[4] = Convert.ToInt16(reader["우편작업cnt"].ToString());   // 선택사항으로 완료/오류/대기가 있음
                            작업건수[5] = Convert.ToInt16(reader["FTP작업cnt"].ToString());    // (선택)main컴만 - 우편,FTP,모바일
                            작업건수[6] = Convert.ToInt16(reader["모바일작업cnt"].ToString());

                            작업건수[최대인수] = Convert.ToInt16(reader["전체작업cnt"].ToString());
                        }
                        //개별1: 약관작업, PDF변환작업, PDF병합작업이 함께 업데이트 된다.(병렬처리로 인해) - 싱글처리도 병렬처리 처럼 작업한다.
                        int 인수 = (개별.StartsWith("개별")) ? Convert.ToInt16(개별.Substring(개별.Length -1)) + 1: 최대인수 - 1; // 일괄 : 모두처리
                        string[] Sql문구 = Enumerable.Repeat("오류", 최대인수).ToArray(); // 각 요소를 '오류'로 채움, 기본오류값으로 하고 완료값으로 변경한다.

                        int 처리Step = 2;
                        //약관작업 단계 : 3단계
                        int 전체건수 = 작업건수[최대인수];
                        bool 레코드오류 = false;
                        for (int i = 0; i <= 인수; i++)
                        {
                            
                            int 해당건수 = 작업건수[i];

                            if (i == 4) {
                                Sql문구[i] = (우편cnt == 해당건수) ? "완료" : "오류";
                            }
                            else if (i == 5 || i == 6 ) //(선택) FTP, 모바일(Table) , -999 : FTP작업, Table작업을 하지 않는다. 따라서 임의로 완료로 처리한다. 
                            {
                                Sql문구[i] = (모바일cnt == -999 || 모바일cnt == 해당건수) ? "완료" : "오류";
                            } else
                            {
                                Sql문구[i] = (전체건수 == 해당건수) ? "완료" : "오류";
                                if(개별 == "개별1" && Sql문구[i] == "오류" && i >=0  && i <= 2)
                                    레코드오류 = true;
                            }

                            처리Step++;
                            상태.처리단계 = string.Format("{0:00}|{1}", 처리Step, Sql문구[i]);
                                                        
                            //약관작업이 오류이면 나머지 모두 오류이다 즉 앞쪽 필드가 오류이면 뒷쪽 모든 필든는 오류이다.
                            if (Sql문구[i] == "오류")  {
                            if(i != 최대인수 - 1)
                                Enumerable.Range(i + 1, 최대인수 - (i+1)).ToList().ForEach(ii => Sql문구[ii] = "대기");
                            break;
                            }
                        }

                        //개별1일때 서버쪽으로 완료/오류 여부를 전달한다
                        if (레코드오류)
                            _lastErrorMsg[1] = "약관/PDF변환,병합 오류";


                        if (개별 == "일괄")
                        {
                            상태.상태 = Sql문구[인수];
                            실행여부 = 상태.상태;
                        } else
                        {
                            상태.상태 = (상태.상태컴퓨터 == "main" && Sql문구[인수] == "완료") ? "처리" : Sql문구[인수];
                        }

                        List<string> 선택쿼리 = new List<string>
                        {
                            $"상태        = '{상태.상태}'",
                            $"처리단계    = '{상태.처리단계}'",
                            $"약관작업    = '약관{Sql문구[0]}'",
                            $"PDF변환작업 = 'PDF변환{Sql문구[1]}'",
                            $"PDF병합작업 = 'PDF병합{Sql문구[2]}'",
                            $"개별PDF작업 = '개별PDF{Sql문구[3]}'",
                            $"우편작업    = '우편{Sql문구[4]}'",
                            $"FTP작업     = 'FTP{Sql문구[5]}'",
                            $"모바일작업  = '모바일{Sql문구[6]}'"
                        };

                        // 필요한 항목까지만 선택 (인수 + 3개 만큼)
                        string 쿼리 = string.Join(", ", 선택쿼리.Take(인수 + 3));
                        
                        string SqlStr = "";
                        SqlStr = $@"
                            UPDATE data처리
                            SET 
                                {쿼리} 
                            WHERE groupKey = '{상태.groupKey}' 
                                     AND 처리구분 = '{상태.처리구분}'
                        ";

                        if (await 실행(() => DBUpdate(SqlStr, 상태.dataTable접속), "병렬처리DBUpdate") == "실패")
                        {
                            throw new Exception("ER00_병렬처리DBUpdate 오류, 로그참조");
                        }

                        this.Invoke(new Action(() =>
                        {
                            if (개별 == "일괄")
                            {
                                로그내용 = string.Format("{0:yy.MM.dd HH:mm:ss} ====================================================================", DateTime.Now);
                                로그기록(로그내용, 1);
                                //약관작업 단계 : 3단계
                                
                                for (int i = 0; i < 선택쿼리.Count; i++)
                                {
                                    string 단계문구 = (i >= 2) ? 단계문구 = $"단계{i:00} : " : "         ";

                                    string 출력문구 = 단계문구 + 선택쿼리[i].Replace("'", "").Replace(",", "");
                                    string 건수문구 = "";
                                    int 상태건수 = 0;
                                    switch (i)
                                    {
                                        case 0:
                                        case 1:
                                            상태건수 = 0;
                                            break;
                                        case 2:
                                        case 3:
                                        case 4:
                                        case 5:
                                            상태건수 = 작업건수[최대인수];
                                            break;
                                        case 6:
                                            상태건수 = 우편cnt;
                                            break;
                                        case 7:
                                        case 8:
                                            상태건수 = (모바일cnt == -999) ? 0 : 모바일cnt;
                                            break;
                                    }
                                    건수문구 = (상태건수 == 0) ? "        :        " : $"({상태건수:00000}) : ({작업건수[i - 2]:00000})";


                                    로그내용 = string.Format("{0:yy.MM.dd HH:mm:ss} [작업처리 상황]{1} ", DateTime.Now, 출력문구.Replace("=",건수문구));
                                    로그기록(로그내용, 1);
                                }
                                로그내용 = string.Format("{0:yy.MM.dd HH:mm:ss} ====================================================================", DateTime.Now);
                                로그기록(로그내용, 1);
                            } else {
                                로그내용 = string.Format("{0:yy.MM.dd HH:mm:ss} [DB완료]data처리 DBUpdate {1} : groupKey - {2} ", DateTime.Now, 상태.처리단계, 상태.groupKey);
                                로그기록(로그내용, 1);
                            }

                        }));
                    }
                    break;
                }
                catch (Exception )
                {
                    재시도++;
                    if (재시도 <= 3)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(5));
                        continue;
                    }
                    실행여부 = "실패";
                    throw;
                }
            }
            return 실행여부;
        }

        private async Task<SqlDataReader> LoadReader(현재상태 상태, string 개별)
        {
            try
            {
                var AdoNet = new SqlConnection(상태.dataTable접속);
                await AdoNet.OpenAsync().ConfigureAwait(false);
                string 조건문구1 = "";
                switch (개별)
                {
                    case "개별1":
                        조건문구1 = "AND 처리구분 = @처리구분";
                        break;
                }
                //우편작업, FTP작업, 모바일작업은 선택사항으로 
                // groupKey : 전체건수 
                var cmd = new SqlCommand($@"
                SELECT
                        COUNT(CASE WHEN 약관작업    = '약관완료'    THEN 1 END) AS 약관작업cnt,
                        COUNT(CASE WHEN PDF변환작업 = 'PDF변환완료' THEN 1 END) AS PDF변환작업cnt,
                        COUNT(CASE WHEN PDF병합작업 = 'PDF병합완료' THEN 1 END) AS PDF병합작업cnt,
                        COUNT(CASE WHEN 개별PDF작업 = '개별PDF완료' THEN 1 END) AS 개별PDF작업cnt,

                        COUNT(CASE WHEN 우편작업    = '우편완료'    THEN 1 END) AS 우편작업cnt,   
                        COUNT(CASE WHEN FTP작업     = 'FTP완료'     THEN 1 END) AS FTP작업cnt,
                        COUNT(CASE WHEN 모바일작업  = '모바일완료'  THEN 1 END) AS 모바일작업cnt,

                        COUNT(CASE WHEN groupKey   <> ''            THEN 1 END) AS 전체작업cnt

                FROM data
                WHERE groupKey = @groupKey 
                      {조건문구1}
                ", AdoNet);
                cmd.Parameters.AddWithValue("@groupKey", 상태.groupKey);
                cmd.Parameters.AddWithValue("@처리구분", 상태.처리구분); // 개별1일때는 실행
                return await cmd.ExecuteReaderAsync(System.Data.CommandBehavior.CloseConnection).ConfigureAwait(false);
            }
            catch (Exception)
            {
                throw;
            }

        }
        
        // ==========================================================================================================
        // 05 - 02: 메인컴은 서브컴이 복사할때 까지 대기한다.
        //          서브컴이 존재하지 않으면 바로 다음단계를 실행 한다.
        // ==========================================================================================================
        private async Task<string> mainCom복사종료확인(현재상태 상태)
        {
            string 실행여부 = "성공";
            string 폴더경로 = System.IO.Path.Combine(상태.출력폴더, "StateFile");
            string 파일이름 = $"{상태.groupKey}.전송완료";
            string 전체경로 = System.IO.Path.Combine(폴더경로, 파일이름);

            const int 최대대기시간초 = 300*10;   // 최대 50분 (필요시 조정)
            const int 체크주기초 = 3;            // 3초마다 보조 체크
            DateTime 시작시간 = DateTime.Now;

            로그내용 = string.Format("{0:yy.MM.dd HH:mm:ss} [대기시작]Client에서 Sever로 File을 전송시작 합니다.", DateTime.Now);
            로그기록(로그내용, 1);

            // 이미 파일이 존재하면 즉시 통과
            if (File.Exists(전체경로))
            {
                로그내용 = string.Format("{0:yy.MM.dd HH:mm:ss} [확인완료]전송완료 파일 이미 존재 : 다음 단계 진행", DateTime.Now);
                로그기록(로그내용, 1);
                return 실행여부;
            }
            // 이벤트 대기 Task
            var watcherTask = new TaskCompletionSource<bool>();

            using (var watcher = new FileSystemWatcher(폴더경로))
            {
                watcher.Filter = 파일이름;
                watcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.CreationTime;
                watcher.Created += (s, e) =>
                {
                    if (e.Name.Equals(파일이름, StringComparison.OrdinalIgnoreCase))
                        watcherTask.TrySetResult(true);
                };
                watcher.EnableRaisingEvents = true;

                bool 감지됨 = false;
                while (!감지됨)
                {
                    // 파일 생성 이벤트 or 타임아웃 중 먼저 도착한 것 확인
                    var 완료 = await Task.WhenAny(watcherTask.Task, Task.Delay(TimeSpan.FromSeconds(체크주기초)));

                    // 이벤트 감지
                    if (완료 == watcherTask.Task) {
                        감지됨 = true;  
                        break;
                    }

                    // 보조 체크 (파일이 직접 생겼는지)
                    if (File.Exists(전체경로)) {
                        감지됨 = true;
                        break;
                    }

                    // 타임아웃 검사
                    if ((DateTime.Now - 시작시간).TotalSeconds >= 최대대기시간초)
                    {
                        실행여부 = "실패";
                        로그내용 = string.Format("{0:yy.MM.dd HH:mm:ss} [오류] {1}초 내에 전송완료 파일이 생성되지 않음", DateTime.Now, 최대대기시간초);
                        로그기록(로그내용, 1);
                        return 실행여부;
                    }

                    // 15초마다 로그 출력 (너무 자주 찍히지 않게)
                    if (((int)(DateTime.Now - 시작시간).TotalSeconds) % 15 == 0)
                    {
                        로그내용 = string.Format("{0:yy.MM.dd HH:mm:ss} [대기중] sub컴에서 파일 이동 대기중...", DateTime.Now);
                        로그기록(로그내용, 1);
                    }
                }
            }

            로그내용 = string.Format("{0:yy.MM.dd HH:mm:ss} [확인완료] sub컴파일 전송완료  → 다음 단계 진행", DateTime.Now);
            로그기록(로그내용, 1);
            return 실행여부;
        }

        // ==========================================================================================================
        // 06 - 증권 + 선택약관 합치기
        //      Bookmarks 생성
        //      모바일(M) 데이타를 가져온다.(성공한 데이타만 가지고 있음)
        // ==========================================================================================================
        private async Task<string> 증권약관합치기(현재상태 상태)
        {
            string 실행여부 = "성공";

            string 약관입력폴더 = $"{상태.출력폴더}Y\\";
            string 기본출력폴더 = 상태.출력폴더;
            string 증권입력폴더 = string.Format("{0}@증권\\{1}\\", 저장.기본입력폴더, 상태.고객사);
            string 증권출력폴더 = "";
            string 북마크 = "bookmarks";

            // 증권처리 후 이동폴더
            //string 증권이동폴더 = 폴더생성(증권입력폴더, "@완료");
            //증권이동폴더 = 폴더생성(증권이동폴더, 상태.groupKey);

            string 증권이동폴더 = $@"{증권입력폴더}@완료\\{DateTime .Now :yyyy.MM}\\{DateTime.Now:dd}\\";
            Directory.CreateDirectory(증권이동폴더);
            증권출력폴더 = 폴더생성(기본출력폴더, "@맞춤증권");
            string 분류폴더 = 폴더생성(증권출력폴더, "P");
            분류폴더 = 폴더생성(증권출력폴더, "M");
            분류폴더 = 폴더생성(증권출력폴더, "E");
            분류폴더 = 폴더생성(증권출력폴더, "L");
            분류폴더 = 폴더생성(증권출력폴더, 북마크);
            분류폴더 = 폴더생성(증권출력폴더, "우편출력");


            Dictionary<string, string> 증권목록 = new Dictionary<string, string>();
            Dictionary<string, string> 약관목록 = new Dictionary<string, string>();
            List<string> 증권파일이동 = new List<string>();

            foreach (FileInfo 파일 in new DirectoryInfo(증권입력폴더).GetFiles("*.pdf")) {
                string tmp고객번호 = System.IO.Path.GetFileNameWithoutExtension(파일.Name); //확장자제거
                증권목록.Add(tmp고객번호, 파일.FullName);
            }
            foreach (FileInfo 파일 in new DirectoryInfo(약관입력폴더).GetFiles("Y*.pdf")) {
                string tmp파일명 = System.IO.Path.GetFileNameWithoutExtension(파일.Name).Substring(1); //확장자제거, 'Y'제거해서 키로 사용
                약관목록.Add(tmp파일명, 파일.FullName);
            }

            try
            {
                var 레코드결과 = from tmp in 레코드s
                            where tmp.개별PDF작업 != "개별PDF완료"
                            orderby tmp.고객번호
                            select tmp;

                int workerCount = 8; // ✅ 동시 실행 제한
                var semaphore = new SemaphoreSlim(workerCount);
                var 레코드목록 = 레코드결과.ToList();

                var 완료고객번호 = new ConcurrentBag<string>();
                var 약관고객번호 = new ConcurrentBag<string>();
                var 오류고객번호 = new ConcurrentBag<string>();
                var 약관오류 = new ConcurrentBag<string>();
                var 증권오류 = new ConcurrentBag<string>();
                var 북마크오류 = new ConcurrentBag<string>();
                int 분류Pcnt = 0;
                int 분류Mcnt = 0;
                int 분류Ecnt = 0;
                int 분류Lcnt = 0;
                int 정상cnt = 0;
                barpdf결합진행.EditValue = 0;
                int[,] 전체집계 = new int[12, 10];

                object 집계락 = new object();

                int lastDoEventsPercent = -1;
                Action<string> 실시간로그출력 = (logMsg) =>
                {
                    // UI 스레드인지 확인 후 Invoke 호출
                    this.Invoke(new Action(() =>
                    {
                        // 1. 로그 찍기
                        로그기록(logMsg, 1);

                        // 2. 진행률 업데이트
                        int 계산 = (레코드목록.Count == 0) ? 0
                                    : Convert.ToInt32((double)정상cnt / (double)레코드목록.Count * 100);
                        barpdf결합진행.EditValue = 계산;

                        // 5%단위로 화면갱신
                        int 몫 = 계산 / 5;
                        if (몫 > 0 && 몫 != lastDoEventsPercent)
                        {
                            lastDoEventsPercent = 몫; // 실행한 퍼센트를 기록
                            Application.DoEvents();     // 화면 갱신
                        }
                    }));
                };


                var tasks = 레코드목록.Select(async 레코드 => {

                    await semaphore.WaitAsync();
                    try
                    {
                        bool 증권만 = 레코드.서식구분 == "03" || 레코드.서식구분 == "3";
                        bool 증권약관 = 레코드.서식구분 == "01" || 레코드.서식구분 == "1";

                        string 메세지 = "";
                        int[,] 개별집계 = null;
                        if (!string.IsNullOrWhiteSpace(레코드.전체약관경로))
                        {
                            //////H2P앱에서 'PDF병합작업' 관련 업데이트를 실행해야 되는데 전체약관은 병합과정이 없어 '대기' 상태로 존재
                            //////이를 업데이트 하기위해 고객번호를 저장하고 '개별PDF작업'에서 완료로 가정하고 업데이틀 한다.
                            ////약관고객번호.Add(레코드.고객번호); 
                            메세지 = "성공";
                        } else {

                            if (!증권만 && !약관목록.TryGetValue(레코드.파일명, out var 약관파일))
                            {
                                약관오류.Add(레코드.고객번호);
                                return;
                            }
                            if (증권약관 && !증권목록.TryGetValue(레코드.고객번호, out var 증권파일))
                            {
                                증권오류.Add(레코드.고객번호);
                                return;
                            }

                            (메세지, 개별집계) = await 개발증권약관병합_PME분리(
                                레코드,
                                증권목록.TryGetValue(레코드.고객번호, out var zf) ? zf : "",
                                약관목록.TryGetValue(레코드.파일명, out var yf) ? yf : ""
                                );
                        }

                        

                        string 로그내용;
                        if (메세지 == "성공" || 메세지 == "북마크")
                        {
                            완료고객번호.Add(레코드.고객번호);
                            레코드.개별PDF작업 = "개별PDF완료";

                            Interlocked.Increment(ref 정상cnt);

                            switch (레코드.발송구분)
                            {
                                case "01":
                                case "1": Interlocked.Increment(ref 분류Pcnt); break; //자동 ++1
                                case "02":
                                case "2": Interlocked.Increment(ref 분류Mcnt); break;
                                case "03":
                                case "3": Interlocked.Increment(ref 분류Ecnt); break;
                                case "04":
                                case "4": Interlocked.Increment(ref 분류Lcnt); break;
                            }

                            lock (집계락)
                            {
                                if (개별집계 != null)
                                {
                                    for (int r = 0; r < 12; r++)
                                        for (int c = 0; c < 10; c++)
                                            전체집계[r, c] += 개별집계[r, c];
                                }
                            }

                            if (메세지 == "북마크")
                            {
                                북마크오류.Add(레코드.고객번호);
                                로그내용 = $"{DateTime.Now:yy.MM.dd HH:mm:ss} [오류] 북마크 생성 오류 - 고객번호 : {레코드.고객번호}";
                            }
                            else
                            {
                                로그내용 = $"{DateTime.Now:yy.MM.dd HH:mm:ss} [완료] 증권 + 약관병합 완료 - 고객번호 : {레코드.고객번호}";
                            }
                        }
                        else
                        {
                            오류고객번호.Add(레코드.고객번호);
                            로그내용 = $"{DateTime.Now:yy.MM.dd HH:mm:ss} [오류] 증권 + 약관병합 오류 - 고객번호 : {레코드.고객번호}, {메세지}";
                        }

                        실시간로그출력(로그내용);

                        //로그큐.Enqueue(로그내용);
                    }
                    catch (Exception ex)
                    {
                        오류고객번호.Add(레코드.고객번호);
                        로그내용 = $"{DateTime.Now:yy.MM.dd HH:mm:ss} [증권약관합치기 예외] {레코드.고객번호} 처리 중 오류: {ex.Message}";
                        실시간로그출력(로그내용);
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                });

                await Task.WhenAll(tasks); // ✅ 전체 작업 대기


                string[] 파일목록 = Directory.GetFiles(증권입력폴더);
                foreach (string 원본파일경로 in 파일목록)
                {
                    string 파일명 = System.IO.Path.GetFileName(원본파일경로);
                    string 이동할파일경로 = System.IO.Path.Combine(증권이동폴더, 파일명);

                    if (File.Exists(이동할파일경로))
                    {
                        File.Delete(이동할파일경로);
                    }
                    File.Move(원본파일경로, 이동할파일경로);
                }



                로그내용 = string.Format("{0:yy.MM.dd HH:mm:ss} ====================================================================", DateTime.Now);
                로그기록(로그내용, 1);
                로그내용 = string.Format("{0:yy.MM.dd HH:mm:ss} [작업건수 상세] === 전체작업   === : {1:000000}", DateTime.Now, 레코드목록.Count());
                로그기록(로그내용, 1);
                로그내용 = string.Format("{0:yy.MM.dd HH:mm:ss} [작업건수     ] === 우편  (P)  === : {1:000000}", DateTime.Now, 분류Pcnt);
                로그기록(로그내용, 1);
                로그내용 = string.Format("{0:yy.MM.dd HH:mm:ss} [작업건수     ] === 모바일(M)  === : {1:000000}", DateTime.Now, 분류Mcnt);
                로그기록(로그내용, 1);
                로그내용 = string.Format("{0:yy.MM.dd HH:mm:ss} [작업건수     ] === 이메일(M)  === : {1:000000}", DateTime.Now, 분류Ecnt);
                로그기록(로그내용, 1);
                로그내용 = string.Format("{0:yy.MM.dd HH:mm:ss} [작업건수     ] === 로컬  (L)  === : {1:000000}", DateTime.Now, 분류Lcnt);
                로그기록(로그내용, 1);
                로그내용 = string.Format("{0:yy.MM.dd HH:mm:ss} ====================================================================", DateTime.Now);
                로그기록(로그내용, 1);
                로그내용 = string.Format("{0:yy.MM.dd HH:mm:ss} [작업건수     ] === 정상작업   === : {1:000000}", DateTime.Now, 완료고객번호.Count);
                로그기록(로그내용, 1);
                로그내용 = string.Format("{0:yy.MM.dd HH:mm:ss} [상세오류건수 ] === 오류작업   === : {1:000000}", DateTime.Now, 오류고객번호.Count);
                로그기록(로그내용, 1);
                로그내용 = string.Format("{0:yy.MM.dd HH:mm:ss} [상세오류건수 ] === 북마크오류 === : {1:000000}", DateTime.Now, 북마크오류.Count);
                로그기록(로그내용, 1);
                로그내용 = string.Format("{0:yy.MM.dd HH:mm:ss} [상세오류건수 ] === 약관오류   === : {1:000000}", DateTime.Now, 약관오류.Count);
                로그기록(로그내용, 1);
                로그내용 = string.Format("{0:yy.MM.dd HH:mm:ss} [상세오류건수 ] === 증권오류   === : {1:000000}", DateTime.Now, 증권오류.Count);
                로그기록(로그내용, 1);
                로그내용 = string.Format("{0:yy.MM.dd HH:mm:ss} ====================================================================", DateTime.Now);
                로그기록(로그내용, 1);

                //      전체     증권+약관     약관만     증권만     
                //P건수  --         --           --         --
                //P장수     
                //    
                // M
                // E
                List<string> 집계타이틀 = new List<string>() {"", "P건수", "P장수", "M건수", "M장수", "E건수", "E장수", "L건수", "L장수", "T건수", "T장수"};
                string 집계파일 = string.Format("{0}@맞춤증권\\({1:yyMMdd_HHmmss})집계.TOT", 상태.출력폴더, DateTime.Now);
                
                using(FileStream fs집계 = new FileStream(집계파일, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Write))
                using (StreamWriter lst집계 = new StreamWriter(fs집계, Encoding.GetEncoding(949)))
                {
                    string 출력 = "";
                    lst집계.WriteLine("========================================================================================");
                    출력 = "            전체        증권+약관   약관만      증권만      상세증권    상세약관";
                    lst집계.WriteLine(출력);
                    lst집계.WriteLine("========================================================================================");
                    for (int r = 1; r <= 10; r++)
                    {
                        var values = Enumerable.Range(0, 6)   // 0 ~ 5 컬럼 반복.
                                               .Select(c => 전체집계[r, c].ToString().PadRight(10, ' '));

                        출력 = 집계타이틀[r].PadRight(10, ' ')
                               + string.Join("\t", values);

                        lst집계.WriteLine(출력);
                    }
                }

                // ===== 여기서 '반드시' DB 업데이트를 수행합니다 (UI 스레드 필요 없음) =====
                if (await 실행(() => BatchUpdateAsync(상태, 완료고객번호, 오류고객번호, "개별PDF"), "증권약관합치기(BatchUpdateAsync)") == "실패")
                {
                    throw new Exception("증권약관합치기(BatchUpdateAsync) 오류, 로그참조");
                }

            }
            catch (Exception)
            {
                실행여부 = "실패";
                throw;
            }

            return 실행여부;
        }

        // ==========================================================================================================
        // 06 - 01 증권 + 선택약관 합치기(현재:약관만은 모바일/이메일만 처리한다.)
        //         목차쪽번호 출력(증권+약관중 약관부분만 출력한다.
        //         Bookmarks 생성
        //         모바일(M) 데이타를 가져온다.(성공한 데이타만 가지고 있음)
        // ==========================================================================================================
        private async Task<(string, int[,] 집계)> 개발증권약관병합_PME분리(레코드 레코드, string 증권파일, string 약관파일)
        {
            string 실행여부 = "성공";
            int[,] 집계 = new int[12, 10];

            int 결합cnt = 0;
            int 증권만cnt = 0;
            int 약관만cnt = 0;
            int 개별증권Cnt = 0;
            
            try
            {
                // 01: 증권 + 약관, 02: 약관만, 03: 증권만
                // 01: 우편,        02: 모바일, 03: 이메일, 04: 로컬

                string tmp서식구분 = "";
                switch (레코드.서식구분)
                {
                    case "01":
                    case "1":
                        tmp서식구분 = "증권약관"; break;
                    case "02":
                    case "2":
                        tmp서식구분 = "약관만"; break;
                    case "03":
                    case "3":
                        tmp서식구분 = "증권만"; break;
                    default:
                        throw new Exception($"[오류] 서식구분 : {레코드.서식구분}");  
                }

                string tmp발송구분 = "";
                switch (레코드.발송구분)
                {
                    case "01": case "1":
                        tmp발송구분 = "P"; break;
                    case "02": case "2": 
                        tmp발송구분 = "M"; break;
                    case "03": case "3": 
                        tmp발송구분 = "E"; break;
                    default:
                        tmp발송구분 = "L"; break;  // 로컬
                }
                
                string userPass = "";
                string owerPass = "";
                bool 패스워드여부 = false;
                string 패스워드 = 레코드.Password;
                WriterProperties setPassword = null;
                List<string> 증권파일이동 = new List<string>();
                bool 북마크여부 = false;
                string 추가경로 = "";
                int 증권PageCnt = 0;
                int 약관PageCnt = 0;

                if (!string.IsNullOrWhiteSpace(패스워드) && 패스워드여부)
                {
                    userPass = 패스워드;
                    if (설정값.TryGetValue($"PDFOWERPASSWORD", out string pass))
                        owerPass = pass;
                    else
                        owerPass = "";

                    setPW(out setPassword, userPass, owerPass);
                    //패스워드여부 = true;
                }
                else
                {
                    setPassword = null;
                }

                if (tmp발송구분 == "P" || tmp서식구분 == "증권만" || 레코드.고객사 == "CH")
                {
                    추가경로 = tmp발송구분;
                } else {
                    북마크여부 = true;  // 북마크를 생성 하기 위해 임시로 \\bookmarks\\ 생성후  북마크를 만들면서 M,E폴더로 이동한다.
                    추가경로 = "bookmarks";
                }

                // 파일명 : 출력경로(우편:P, 모바일:M, 이메일:E  로컬:L 또는 bookmarks) + 발송구분 + 처리일자 + 상품코드 + 개정일 + 증권번호
                string 출력파일 = string.Format("{0}@맞춤증권\\{1}\\{2}{3}.pdf", 레코드.출력폴더, 추가경로, tmp발송구분, 레코드.파일명);
                using (var fsw = new FileStream(출력파일, FileMode.Create, FileAccess.Write, FileShare.Write, 4096, false))
                {
                    // ========================================================
                    //      증권만 형태     
                    // ========================================================
                    if (tmp서식구분 == "증권만")
                    {
                        using (var 증권r = new FileStream(증권파일, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, false))
                        using (var 개별증권doc = new PdfDocument(new PdfReader(증권r), new PdfWriter(fsw)))
                        {
                            // ===============================
                            //   주소PDF 맨뒤로 보내기 (1/3) - 우편발송 시 주소페이지가 불투명(해결할것)
                            // ===============================
                            증권만cnt = 개별증권doc.GetNumberOfPages();
                            if (증권만cnt % 2 == 1 && tmp발송구분 == "P")
                            {
                                // 증권에 주소가 앞에 있는 경우와 뒤에 있는 경우가 있다.
                                // 롯데손해보험은 앞에, 나머지(MG,KB)는 뒤에 있다
                                // 빈페이지 삽입
                                var pageSize = 개별증권doc.GetPage(1).GetPageSizeWithRotation();
                                if (레코드.고객사 == "LT") {
                                    개별증권doc.AddNewPage((PageSize)pageSize);
                                } else {
                                    개별증권doc.AddNewPage(증권만cnt, (PageSize)pageSize);
                                }
                                증권만cnt++;
                            }
                        }
                    }
                    // ========================================================
                    //      약관만 형태
                    // ========================================================
                    else if (tmp서식구분 == "약관만"){

                        using (var 약관r = new FileStream(약관파일, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, false))
                        using (var 개별증권doc = new PdfDocument(new PdfReader(약관r), new PdfWriter(fsw)))
                        {

                            약관만cnt = 개별증권doc.GetNumberOfPages();
                            if (tmp발송구분 == "P")
                            {
                                // 1. 우편짝수 빈 페이지 추가 (원래는 홀수 일때 추가지만 주소페이지를 짝수페이지에 출력하기 위해
                                var pageSize = 개별증권doc.GetPage(1).GetPageSizeWithRotation();
                                // KB손보는 민원고객에 한해 증권만 우편으로 발송한다.
                                // 주소PDF가 없어 별도로 주소PDF를 생성한다. 
                                
                                if (레코드.tmp03 == "1")
                                {
                                    if (약관만cnt % 2 == 0) //주소PDF를 생성하기 위해
                                    {
                                        개별증권doc.AddNewPage((PageSize)pageSize);
                                        약관만cnt++;
                                    }

                                    // 2. 주소 출력용 페이지 생성
                                    PdfPage newPage = 개별증권doc.AddNewPage((PageSize)pageSize);
                                    약관만cnt++;
                                    Document docw = new Document(개별증권doc);

                                    string 폰트경로1 = @"c:\Easy약관Plus\program\Font\KBFGDISPLAYM.TTF";
                                    string 폰트경로2 = @"c:\Easy약관Plus\program\Font\KBFGDISPLAYB.TTF";

                                    iText.Kernel.Font.PdfFont KB폰트_M = iText.Kernel.Font.PdfFontFactory.CreateFont(폰트경로1, iText.IO.Font.PdfEncodings.IDENTITY_H);
                                    iText.Kernel.Font.PdfFont KB폰트_B = iText.Kernel.Font.PdfFontFactory.CreateFont(폰트경로2, iText.IO.Font.PdfEncodings.IDENTITY_H);

                                    iText.Kernel.Pdf.Canvas.PdfCanvas pdfCanvas = new iText.Kernel.Pdf.Canvas.PdfCanvas(newPage);
                                    iText.Layout.Canvas canvas = new iText.Layout.Canvas(pdfCanvas, newPage.GetPageSize());

                                    string 주소1 = "";
                                    string 주소2 = "";
                                    string 고객명 = 레코드.고객명;
                                    string 우편번호 = string.Join(" ", 레코드.우편번호.ToCharArray());

                                    if (레코드.주소.Length > 0)
                                    {
                                        Encoding enc = Encoding.GetEncoding("euc-kr");
                                        string[] 주소분리 = 레코드.주소.Split(' ');

                                        int left = 0;                 // 앞에서부터 증가할 인덱스
                                        int right = 주소분리.Length - 1; // 뒤에서부터 감소할 인덱스

                                        while (left <= right)
                                        {
                                            int len1 = enc.GetByteCount(주소1);
                                            int len2 = enc.GetByteCount(주소2);

                                            // 3. 바이트 길이를 비교하여 짧은 쪽에 단어 배분
                                            if (len1 <= len2)
                                            {
                                                주소1 += 주소분리[left] + " "; // 주소1은 뒤에 이어 붙임
                                                left++;                        // 증가
                                            }
                                            else
                                            {
                                                // 주소2는 뒤에서 가져온 단어이므로 문자열 앞에 붙여야 원래 순서가 유지됨
                                                주소2 = 주소분리[right] + " " + 주소2;
                                                right--;                       // 감소
                                            }
                                        }
                                    }

                                    Paragraph p주소1 = new Paragraph(주소1.Trim()).SetFont(KB폰트_M).SetFontSize(10);
                                    Paragraph p주소2 = new Paragraph(주소2.Trim()).SetFont(KB폰트_M).SetFontSize(10);
                                    Paragraph p고객명 = new Paragraph(고객명).SetFont(KB폰트_B).SetFontSize(12); // 크고 진하게
                                    Paragraph p귀하 = new Paragraph("귀하").SetFont(KB폰트_M).SetFontSize(10);
                                    Paragraph p우편번호 = new Paragraph(우편번호).SetFont(KB폰트_M).SetFontSize(11);

                                    double xStart = 185.0; // 기준 : 왼쪽 아래
                                    double yAlign = 297 - 85;
                                    int 위치 = 개별증권doc.GetNumberOfPages();

                                    주소출력(docw, xStart, yAlign, 위치, p주소1, TextAlignment.RIGHT);
                                    주소출력(docw, xStart, yAlign - 4.0, 위치, p주소2, TextAlignment.RIGHT);
                                    주소출력(docw, xStart, yAlign - 11.0, 위치, p귀하, TextAlignment.RIGHT);
                                    주소출력(docw, xStart - 8, yAlign - 11.0, 위치, p고객명, TextAlignment.RIGHT);
                                    주소출력(docw, xStart, yAlign - 18.0, 위치, p우편번호, TextAlignment.RIGHT);
                                    docw.Close();
                                }
                                else
                                {
                                    if (약관만cnt % 2 != 0)
                                    {
                                        개별증권doc.AddNewPage((PageSize)pageSize);
                                        약관만cnt++;
                                    }
                                }
                                
                            }
                        }
                    }
                    // ========================================================
                    //      증권+약관 형태
                    // ========================================================
                    else if (tmp서식구분 == "증권약관")
                    {
                        using (var 증권Stream = new FileStream(증권파일, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, false))
                        using (var 약관Stream = new FileStream(약관파일, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, false))
                        using (var 증권Doc = new PdfDocument(new PdfReader(증권Stream)))
                        using (var 약관Doc = new PdfDocument(new PdfReader(약관Stream)))
                        using (var 개별증권doc = new PdfDocument(new PdfWriter(fsw)))
                        using (var docw = new Document(개별증권doc)) {
                            PdfMerger merger = new PdfMerger(개별증권doc).SetCloseSourceDocuments(false);   //Merge 함수는 머지를 한 후 파일을 자동으로 닫는다. 계속 Open하려면 SetCloseSourceDocuments 값을 false로 설정 

                            //if (패스워드여부)
                            //{
                            //    개별증권doc = new PdfDocument(new PdfWriter(fsw, setPassword));
                            //}
                            //else
                            //{
                            //    개별증권doc = new PdfDocument(new PdfWriter(fsw));
                            //}

                            증권PageCnt = 증권Doc.GetNumberOfPages();
                            약관PageCnt = 약관Doc.GetNumberOfPages();

                            //// ========================================================
                            ////   주소PDF 맨뒤로 보내기 (2/2) - ①      
                            //// ========================================================
                            // 증권에 주소가 앞에 있는 경우와 뒤에 있는 경우가 있다.
                            // 롯데손해보험은 앞에, 나머지(MG,KB)는 뒤에 있다
                            if (레코드.고객사 == "LT") {
                                merger.Merge(증권Doc, 1, 증권PageCnt);
                            } else {
                                merger.Merge(증권Doc, 1, 증권PageCnt - 1); //맨뒷장 주소페이지 제외 - 약관을 병합하고 추후 주소페이지를 옮긴다.
                            }

                            iText.Kernel.Geom.Rectangle pagesize = 개별증권doc.GetPage(1).GetPageSizeWithRotation();
                            if (개별증권doc.GetNumberOfPages() % 2 != 0 && tmp발송구분 == "P")
                            {
                                // 빈페이지 삽입
                                개별증권doc.AddNewPage((PageSize)pagesize);
                            }

                            // ========================================================
                            //         약관결합
                            // ========================================================
                            // 약관 짝/홀수는 약관생성 하는 곳에 처리함
                            merger.Merge(약관Doc, 1, 약관PageCnt);

                            // ========================================================
                            //    목차쪽번호 출력
                            // ========================================================

                            if (await 실행(() => 목차쪽번호출력(docw, 개별증권doc, 약관PageCnt), "목차쪽번호출력") == "실패")
                            {
                                throw new Exception("목차쪽번호출력 오류, 로그참조");
                            }

                            // ========================================================
                            //   주소PDF 맨뒤로 보내기 (2/2) - ②    
                            // ========================================================
                            // 증권 n페이지는 주소 페이지 
                            // 짝수페이지로 모든PDF를 setting한다.
                            // 짝수페이지를 n-1로 해서 주소가 있으면 n번째 위치하게 한다.
                            // 주소페이지를 맨뒤로 출력한다.

                            int 개별증권pagecnt = 개별증권doc.GetNumberOfPages();
                            if (레코드.고객사 == "LT")
                            {   //짝수
                                if (개별증권pagecnt % 2 != 0 && tmp발송구분 == "P"){
                                    개별증권doc.AddNewPage((PageSize)pagesize);
                                }
                            } 
                            else
                            {   //홀수
                                if (개별증권pagecnt % 2 == 0 && tmp발송구분 == "P"){
                                    개별증권doc.AddNewPage((PageSize)pagesize);
                                }
                                merger.Merge(증권Doc, 증권PageCnt, 증권PageCnt);  // 주소페이지 맨뒤에 병합
                            }

                            개별증권Cnt = 개별증권doc.GetNumberOfPages();
                        }
                    }
                }
                
                int 행 = Convert.ToInt32(레코드.발송구분) * 2 - 1;
                int 열 = Convert.ToInt32(레코드.서식구분);
                int 장수 = 0;

                if (tmp서식구분 == "증권만" || tmp서식구분 == "약관만")
                {
                    // ========================================================
                    //       집계 (1/2)
                    // ========================================================

                    집계[행, 0]++;          // 항목합계(P,M,E,L) 건수
                    집계[행, 열]++;         // 항목별 - 증권+약관, 약관만, 증권만 건수

                    집계[행 + 1, 0] += (int)Math.Ceiling((double)증권만cnt / 2.0) + (int)Math.Ceiling((double)약관만cnt / 2.0);        // 항목합계(P,M,E) 장수

                    if (열 == 2) {
                        장수 = (int)Math.Ceiling((double)약관만cnt / 2.0);                           // 항목합계(P,M,E) 장수 - 약관장수
                        집계[행 + 1, 열] += 장수;
                    } else  {
                        장수 = (int)Math.Ceiling((double)증권만cnt / 2.0);                           // 항목합계(P,M,E) 장수 - 증권장수
                        집계[행 + 1, 열] += 장수;
                    }

                    집계[9, 0]++;
                    집계[9, 열]++;
                    집계[10, 열] += 장수;
                    집계[10, 0] = 집계[10, 1] + 집계[10, 2] + 집계[10, 3];
                }
                if (tmp서식구분 == "증권약관")
                {
                    // ========================================================
                    //       집계 (2/2)
                    // ========================================================
                    집계[행 + 1, 4] += (int)Math.Ceiling((double)증권PageCnt / 2.0);     // 항목합계(P,M,E) 장수 - 증권장수

                    집계[행 + 1, 5] += (int)(Math.Ceiling((double)개별증권Cnt / 2.0)) - (int)Math.Ceiling((double)증권PageCnt / 2.0);     // 항목합계(P,M,E) 장수 - 약관장수

                    집계[행, 0]++;                                                                             // 항목합계(P,M,E) 건수
                    집계[행, 열]++;                                                                            // 항목별 - 증권+약관, 약관만, 증권만 건수

                    int 장수1 = (int)Math.Ceiling((double)개별증권Cnt / 2.0);        // 항목합계(P,M,E) 장수
                    집계[행 + 1, 0] += 장수1;

                    int 장수2 = (int)Math.Ceiling((double)개별증권Cnt / 2.0);       // 항목별 - 증권+약관, 약관만, 증권만 장수
                    집계[행 + 1, 열] += 장수2;

                    집계[9, 0]++;
                    집계[9, 열]++;


                    집계[10, 열] += 장수2;

                    집계[10, 4] += (int)Math.Ceiling((double)증권PageCnt / 2.0);
                    집계[10, 5] += (int)(Math.Ceiling((double)개별증권Cnt / 2.0)) - (int)Math.Ceiling((double)증권PageCnt / 2.0);

                    집계[10, 0] = 집계[10, 1] + 집계[10, 2] + 집계[10, 3];
                }

                // ========================================================
                //    북마크생성
                // ========================================================
                // 라이나손해보험은 제외한다.(목차가 생성되어 있지 않음)
                if (북마크여부)
                {
                    string 북마크제목 = string.Format("♣ {0} 고객님의 {1} ♣", 레코드.고객명, (tmp서식구분 == "증권약관") ? "보험증권, 맞춤약관" : "맞춤약관");
                    string 북마크입력파일 = 출력파일;
                    string 북마크출력파일 = string.Format("{0}@맞춤증권\\{1}\\{1}{2}.pdf", 레코드.출력폴더, tmp발송구분, 레코드.파일명);
                    
                    if (await 실행(() => 북마크생성(북마크입력파일, 북마크출력파일, 북마크제목, setPassword), "북마크생성") != "성공")
                    {
                        // 북마크오류가 발생해도 M,E을 발송한다.
                        // /bookmarks/해당파일을 => /출력폴더/@맞춤증권/M 폴더로 이동한다.
                        new FileInfo(북마크입력파일).CopyTo(북마크출력파일);
                        new FileInfo(북마크입력파일).Delete();
                        실행여부 = "북마크";
                        throw new Exception("북마크");  // 강제로 Exception 발생 후 아래로직에서 업데이트 후 다음레코드 처리
                    }

                }

                결합cnt++;
            }
            catch (Exception e1)
            {
                실행여부 = e1.Message;
            }
            return (실행여부, 집계);
        }
        
        private void 주소출력(Document docw, double X, double Y, int 위치, Paragraph 항목, TextAlignment 정렬)
        {
            docw.ShowTextAligned(항목, (float)MMtoPoint(X), (float)MMtoPoint(Y), 위치, 정렬, VerticalAlignment.BOTTOM, 0);
        }

        // ==========================================================================================================
        // 06 - 02 목차쪽번호 출력(증권+약관중 약관부분만 출력한다.
        // ==========================================================================================================
        private async Task<string> 목차쪽번호출력(Document docw, PdfDocument 개별증권doc, int 약관PageCnt)
        {
            return await Task.Run(() =>
            {
                string 실행여부 = "성공";
                try
                {
                    PdfFont 폰트 = PdfFontFactory.CreateFont(
                        @"c:\Easy약관Plus\program\Font\NanumBarunGothic.ttf",
                        PdfEncodings.IDENTITY_H);

                    docw.SetFontSize(10).SetFont(폰트).SetFontColor(ColorConstants.BLACK);

                    int start = 개별증권doc.GetNumberOfPages() - 약관PageCnt + 1;
                    int end = 개별증권doc.GetNumberOfPages();

                    float x = MMtoPoint(105);
                    float y = MMtoPoint(5.5f);

                    int 번호 = 0;
                    for (int j = start; j <= end; j++)
                    {
                        번호++;
                        string 일련번호 = string.Format("- {0} -", 번호);
                        docw.ShowTextAligned(new Paragraph(일련번호), x, y, j,
                            TextAlignment.CENTER, VerticalAlignment.BOTTOM, 0);
                    }
                }
                catch (Exception)
                {
                    실행여부 = "실패";
                    throw;
                }
                return 실행여부;
            });
        }

        // ==========================================================================================================
        // 06 - 03 목차Bookmarks 생성
        // ==========================================================================================================
        private async Task<string> 북마크생성(string 입력, string 출력, string 북마크제목, WriterProperties setPassword)
        {
            string 실행여부 = "성공";
            try
            {
                List<string> 편집 = new List<string>();
                using (PdfDocument pdfDoc = new PdfDocument(new PdfReader(입력))) {

                    PdfNameTree destsTree = pdfDoc.GetCatalog().GetNameTree(PdfName.Dests);
                    PdfOutline outlines = pdfDoc.GetOutlines(false);
                    if (outlines == null)
                    {
                        실행여부 = "실패";
                        throw new Exception("북마크 생성오류, 로그참조");
                    }
                    IList<PdfOutline> bookmarks = outlines.GetAllChildren();
                    int 묶음cnt = 0;

                    foreach (PdfOutline bookmark in bookmarks)
                    {
                        string 저장 = "아니요";
                        묶음cnt++;
                        bool 파일명제외 = true; // 파일결합 때문에 목차 1SETP에 파일이 표시됨 (제외하기위해)

                        실행여부 = await 실행(() => 북마크타이틀(
                            bookmark,
                            destsTree.GetNames(),
                            pdfDoc, stepCnt: 0,
                            편집: 편집,
                            저장: 저장,
                            묶음cnt: 묶음cnt,
                            파일명제외: 파일명제외), "북마크타이틀");
                     
                        if (실행여부 != "성공")
                            return 실행여부;
                    }
                }

                // 암호 설정 여부에 따른 PDF 생성
                using (PdfDocument pdfDoc = (setPassword != null)
                    ? new PdfDocument(new PdfReader(입력), new PdfWriter(출력, setPassword))
                    : new PdfDocument(new PdfReader(입력), new PdfWriter(출력)))
                {

                    실행여부 = await 실행(() => 북마크상세출력(pdfDoc, 편집, 북마크제목), "북마크상세출력");
                    if (실행여부 != "성공")
                        return 실행여부;
                }
            }
            catch (Exception)
            {
                실행여부 = "실패";
                
            }
            return 실행여부;
        }

        private async Task<string> 북마크타이틀(PdfOutline outline, IDictionary<string, PdfObject> result, PdfDocument pdfDoc,
                                    int stepCnt, List<string> 편집, string 저장, int 묶음cnt, bool 파일명제외)
        {
            string 실행여부 = "성공";
            try
            {
                if (outline != null)
                {
                    string bookmarkTitle = "";
                    int 페이지 = -1;
                    bool 목차페이지없음 = false;

                    try
                    {
                        bookmarkTitle = outline.GetTitle();
                        페이지 = pdfDoc.GetPageNumber((PdfDictionary)outline.GetDestination().GetDestinationPage(result));    // 목차정보에 가끔 목차타이틀은 있는데 분기하는 페이지번호가 없다.
                    }
                    catch (Exception) {
                        목차페이지없음 = true;
                    }


                    if (파일명제외 == true ||
                        bookmarkTitle.Trim().Replace(" ", "") == "(내용없음)" ||
                        bookmarkTitle.Trim().Replace(" ", "") == "개요" ||
                        목차페이지없음 == true
                        ) {
                        저장 = "아니요";
                    }

                    파일명제외 = false; // 파일결합으로 목차 1step에 파일명이 존재한다.(제외하기위해)
                    if (저장 == "예")
                    {
                        // PDF세션별로 묶음을 하면 bookmarks가 기존에 생성 되어 있어 '목차'를 삽입해도 목차타이틀이 맨뒤로 붙는다.
                        // bookmarks에서 목차를 페이지 맞게 위치를 변경해서 삽입한다.

                        stepCnt++;
                        if (bookmarkTitle.Trim().Replace(" ", "") == "목차")
                        {
                            for (int i = 0; i < 편집.Count - 1; i++)
                            {
                                int S = Convert.ToInt32(편집[i].Split('|')[3].Trim());   // 4번째 값
                                int E = Convert.ToInt32(편집[i + 1].Split('|')[3].Trim());

                                // S < 페이지 < E
                                if (페이지 > S && 페이지 < E) {
                                    편집.Insert(i + 1, (string.Format("{0:000}|{1:000}|{2}|{3}", 묶음cnt, stepCnt, bookmarkTitle.Trim(), 페이지)));
                                    break;
                                }
                            }

                        } else {
                            편집.Add(string.Format("{0:000}|{1:000}|{2}|{3}", 묶음cnt, stepCnt, bookmarkTitle.Trim(), 페이지));
                        }
                    }

                    // '개요' 다음부터 저장을 한다.
                    if (bookmarkTitle.Trim().Replace(" ", "") == "개요" || 
                        bookmarkTitle.Trim().Replace(" ", "") == "(내용없음)" || 
                        목차페이지없음 == true)
                    {
                        저장 = "예";
                    }

                    IList<PdfOutline> kids = outline.GetAllChildren();
                    if (kids != null) {
                        foreach (PdfOutline kid in kids)
                        {
                            실행여부 = await 실행(() => 북마크타이틀(kid, result, pdfDoc, stepCnt, 편집, 저장, 묶음cnt, 파일명제외), "북마크타이틀");
                            if (실행여부 != "성공")
                                return 실행여부;
                        }
                    }
                }

            }
            catch (Exception)
            {
                실행여부 = "실패";
                throw;
            }
            return 실행여부;
        }

        private async Task<string> 북마크상세출력(PdfDocument pdfDoc, List<string> 편집, string 북마크제목)
        {
            string 실행여부 = "성공";
            try
            {
                // 북마크제목 <===== ♣홍길동 고객님의 보험증권, 맞춤약관♣
                pdfDoc.GetOutlines(true).RemoveOutline();
                PdfOutline rootOutline = pdfDoc.GetOutlines(true);
                PdfOutline 기본목차 = rootOutline.AddOutline(북마크제목);
                기본목차.AddDestination(PdfExplicitDestination.CreateFit(pdfDoc.GetPage(1)));

                List<string> 세션묶음 = new List<string>();
                string 비교묶음 = 편집[0].Substring(0, 3);  // ex) 004|004|【 상해관련 특별약관 】63
                string 현재묶음 = 편집[0].Substring(0, 3);

                for (int i = 0; i < 편집.Count; i++)
                {
                    현재묶음 = 편집[i].Substring(0, 3);
                    if (현재묶음 != 비교묶음)
                    {
                        실행여부 = await 실행(() => 하위북마크생성(기본목차, 세션묶음, pdfDoc), "하위북마크생성");
                        if (실행여부 != "성공")
                            return 실행여부;

                        세션묶음 = new List<string>();
                        비교묶음 = 현재묶음;
                    }

                    세션묶음.Add(편집[i]);

                    if (i == 편집.Count - 1)
                    {
                        실행여부 = await 실행(() => 하위북마크생성(기본목차, 세션묶음, pdfDoc), "하위북마크생성");
                        if (실행여부 != "성공")
                            return 실행여부;
                    }
                }
            }
            catch (Exception) {
                실행여부 = "실패";
                
            }
            return 실행여부;
        }

        private async Task<string> 하위북마크생성(PdfOutline 기본목차, List<string> 세션묶음, PdfDocument pdfDoc)
        {
            string 실행여부 = "성공";
            int 확인i = -1;
            try
            {
                List<PdfOutline> lst목차 = new List<PdfOutline>();
                lst목차.Add(기본목차);
                

                PdfOutline tmp목차;

                int 기준차수 = 0;
                int 현재차수 = 0;
                int 차이차수 = -1;

                for (int i = 0; i < 세션묶음.Count; i++)
                {
                    확인i = i;
                    string[] 분리 = 세션묶음[i].Split('|').Select(P => P.Trim()).ToArray();
                    if (i == 0) { 차이차수 = Convert.ToInt32(분리[1]) - 1; }

                    int tmp현재차수 = Convert.ToInt32(분리[1]) - 차이차수;
                    현재차수 = tmp현재차수 <= 0 ? 1 : tmp현재차수;
                    tmp목차 = lst목차[현재차수 - 1].AddOutline(분리[2]);
                    tmp목차.AddDestination(PdfExplicitDestination.CreateFit(pdfDoc.GetPage(Convert.ToInt32(분리[3]))));

                    if (기준차수 < 현재차수)
                    {
                        lst목차.Add(tmp목차);
                        기준차수 = 현재차수;
                    }
                    else
                    {
                        lst목차[현재차수] = tmp목차;
                    }

                }
            }
            catch (Exception ex)
            {
                string exm = $"{확인i} {ex.Message}" ;
                await Task.Delay(TimeSpan.FromSeconds(0.1));
                실행여부 = "실패";
                
            }
            return 실행여부;
        }

        // ==========================================================================================================
        // 06 - 04 data, data처리 테이블 Update : 개별PDF작업
        //         100개씩 배치 업데이트
        // ==========================================================================================================
        private async Task<string> BatchUpdateAsync(현재상태 상태, ConcurrentBag<string> 완료, ConcurrentBag<string> 오류, string 현상태)
        {
            return await NetworkManager.BatchUpdateAsync(상태, 완료, 오류, 현상태, (msg, level) =>
            {
                로그기록(msg, level);
            });

        }

        // =========================================================================
        // NetworkManager 연결용 껍데기(Wrapper) 함수들
        // =========================================================================

        private async Task<string> FTP_FILE_UPLOAD2(현재상태 상태)
        {
            // 폼에 있는 전역변수(레코드s, 설정값)와 화면 제어 함수(로그기록 등)를 포장해서 던져줍니다.
            return await NetworkManager.FTP_FILE_UPLOAD2(
                상태,
                레코드s,
                (msg, level) => 로그기록(msg, level),
                (progress) => 담보Merge메인화면_SetMessage?.Invoke(progress)
            );
        }

        private async Task<string> FTP_FILE_UPLOAD(현재상태 상태)
        {
            return await NetworkManager.FTP_FILE_UPLOAD(
                상태,
                레코드s,
                (msg, level) => 로그기록(msg, level),
                (progress) => 담보Merge메인화면_SetMessage?.Invoke(progress)
            );
        }

        private async Task<string> 모바일데이터업로드(현재상태 상태)
        {
            // 모바일 데이터 업로드는 프로그레스바가 필요 없으므로 4개만 던집니다.
            return await NetworkManager.모바일데이터업로드(
                상태,
                레코드s,
                (msg, level) => 로그기록(msg, level)
            );
        }


        // ==========================================================================================================
        // 07 - 우편발송 일때 우편폴더에 있는 파일을 출력에 용이하게 1개로 묶는다.
        //      현재상품별로 되어 있으나 추후 장수별로 묶어야 함
        //      100단위로 묶고 라벨을 출력한다.
        // ==========================================================================================================
        private async Task<string> 우편출력_파일병합(현재상태 상태)
        {
            //우편파일을 하나로 묶는다.
            string 실행여부 = "성공";
            try
            {

                //확인-1번 : 리스트에 우편 데이터가 존재하는지 확인 
                if (!레코드s.Any(x => (x.발송구분 == "01" || x.발송구분 == "1") && x.우편작업 != "우편완료"))
                {
                    // 조건에 맞는 레코드가 하나도 없음
                    실행여부 = "성공";  //리스트에 데이터가 없을 뿐 에러는 아님
                    throw new Exception("ListFile에 우편 데이터 미존재, 우편출력_파일병합");
                }

                string 우편폴더 = string.Format("{0}@맞춤증권\\P\\", 상태.출력폴더);
                if (new DirectoryInfo(우편폴더).GetFiles("P*.pdf").Count() == 0)
                {
                    //ListFile에는 데이터가 있는데 P폴더에는 파일이 없는경우로 에러
                    throw new Exception("P폴더에 파일 미존재, FTP전송 중단");
                }
                else
                {
                    string 우편출력 = string.Format("{0}@맞춤증권\\우편출력\\", 상태.출력폴더);
                    foreach (var 삭제 in new DirectoryInfo(우편출력).GetFiles("*.*"))
                        삭제.Delete();
                }

                단계진행문구출력("07-A");

                우편s = new ConcurrentBag<우편파일>();

                실행여부 = await 실행(() => PDF장수확인(우편폴더, 우편s), "PDF장수확인");
                if (실행여부 != "성공")
                {
                    throw new Exception(실행여부);
                }

                // 고객번호 기준으로 우편 데이터를 Dictionary로 변환
                var 우편Dict = 우편s.ToDictionary(u => u.고객번호);
                foreach (var item in 레코드s)
                {
                    if (우편Dict.TryGetValue(item.고객번호, out var 우편))
                    {
                        item.우편출력파일 = 우편.우편출력파일;
                        //item.우편PDF장수 = (int)Math.Ceiling(우편.우편페이지 / 2.0);
                        item.우편PDF장수 = 우편.우편페이지;
                    }
                }

                // 1차: "03" 또는 "3" / 2차: "01" 또는 "1"
                string[][] 서식조건그룹 = new string[][]
                {
                    new string[] { "03", "3" },
                    new string[] { "02", "2" },
                    new string[] { "01", "1" }
                };

                int 총처리건수 = 0;
                foreach (var 조건 in 서식조건그룹)
                {
                    var 장수구간별그룹 = from tmp in 레코드s
                                  where (tmp.발송구분 == "01" || tmp.발송구분 == "1")
                                      && (tmp.서식구분 == 조건[0] || tmp.서식구분 == 조건[1])
                                  // 핵심 1: (페이지수 + 1) / 2 를 통해 '장수'를 계산
                                  // 핵심 2: 계산된 장수로 (장수 - 1) / 10 을 적용하여 10단위 그룹 생성
                                  group tmp by (((Math.Max(1, tmp.우편PDF장수) + 1) / 2) - 1) / 10 into g
                                  select new
                                  {
                                      // 구간명칭 생성 (예: 0001_0010, 0011_0020)
                                      장수구간명 = string.Format("{0:0000}_{1:0000}", (g.Key * 10) + 1, (g.Key * 10) + 10),

                                      // 그룹 내에서 장수(페이지수), 고객번호 순으로 정렬
                                      레코드목록 = g.OrderBy(x => x.우편PDF장수).ThenBy(x => x.고객번호).ToList()
                                  };

                    // 장수 구간별로 반복하며 각각 병합 실행
                    foreach (var 그룹 in 장수구간별그룹)
                    {
                        if (그룹.레코드목록.Count > 0)
                        {
                            // 우편파일병합 파라미터로 "장수구간명"을 넘겨줍니다.
                            if (await 실행(() => 우편파일병합(그룹.레코드목록, 상태, 그룹.장수구간명), "우편파일병합") != "성공")
                            {
                                throw new Exception($"우편파일병합 작업오류(서식구분:{조건[0]},{조건[1]} / 장수구간:{그룹.장수구간명}), 로그참조");
                            }

                            총처리건수 += 그룹.레코드목록.Count;
                        }
                    }
                }

                로그내용 = string.Format("{0:yy.MM.dd HH:mm:ss} [완료] 우편출력_파일병합 완료 : 우편{1:0000}", DateTime.Now, 총처리건수);
                로그기록(로그내용, 1);
            }
            catch (Exception)
            {
                실행여부 = "실패";
                throw;
            }

            return 실행여부;
        }

        // ==========================================================================================================
        // 07 - 01 출력폴더/P/P*.PDF 파일을 읽어 페이지수를 저장한다.
        // ==========================================================================================================
        private async Task<string> PDF장수확인(string 우편폴더, ConcurrentBag<우편파일> 우편s)
        {
            string 실행여부 = "성공";
            try
            {
                var semaphore = new SemaphoreSlim(Environment.ProcessorCount); // 동시 실행 개수 제한
                var tasks = new List<Task>();

                foreach (FileInfo 파일 in new DirectoryInfo(우편폴더).GetFiles("P*.pdf"))
                {
                    tasks.Add(Task.Run(async () =>
                    {
                        await semaphore.WaitAsync();
                        try
                        {
                            string[] tmp파일이름분리 = System.IO.Path.GetFileNameWithoutExtension(파일.Name).Split('_');
                            string tmp상품코드 = tmp파일이름분리[1];
                            string tmp고객번호 = string.Join("_", tmp파일이름분리, 2, tmp파일이름분리.Length - 2);

                            int 페이지수 = 0;
                            using (var reader = new PdfReader(파일.FullName))
                            using (var pdf = new PdfDocument(reader))
                            {
                                페이지수 = pdf.GetNumberOfPages();
                            }
                            우편s.Add(new 우편파일()
                            {
                                고객번호 = tmp고객번호,
                                우편출력파일 = 파일.FullName,
                                우편페이지 = 페이지수,
                                상품코드 = tmp상품코드
                            });
                        }
                        catch (Exception)
                        {
                            throw;
                        }
                        finally
                        {
                            semaphore.Release();
                        }
                    }));
                }
                await Task.WhenAll(tasks);
            }
            catch (Exception)
            {
                실행여부 = "실패";
                throw;
            }
            return 실행여부;
        }

        // ==========================================================================================================
        // 07 - 02 장수 오름차순 100단위 묶음 
        //         우편PDF파일 병합
        // ==========================================================================================================
        private async Task<string> 우편파일병합(List<레코드> 레코드목록, 현재상태 상태, string 장수구간명)
        {
            string 실행여부 = "성공";
            try
            {
                int batchSize = 100;
                int 파일단위 = 0;
                int 시작page = 1;
                int 종료page = 0;
                int 그룹page = 0;
                int 출력면수 = 0;
                int 장수 = 0;
                int MAXpage = 0;
                int MINpage = 0;
                int lst시작 = 1;
                int lst종료 = 0;
                double RollCnt = 2;  // 라벨 2페이지가 추가 되므로 
                
                string 우편출력경로 = string.Format("{0}@맞춤증권\\우편출력\\", 상태.출력폴더);
                //tmp서식구분 == 03 이면 증권만
                string 증권만문구 = "";
                if (레코드목록[0].서식구분 == "03" || 레코드목록[0].서식구분 == "3")
                {
                    증권만문구 = "(증권만)";
                    우편출력경로 = System.IO.Path.Combine(우편출력경로, "증권만\\");
                    if (!System.IO.Directory.Exists(우편출력경로))
                        System.IO.Directory.CreateDirectory(우편출력경로);
                }

                string 리스트파일 = string.Format("{0}({1:MMdd})_{2}_우편{3}.lst", 우편출력경로, DateTime.Now, 장수구간명, 증권만문구);
                FileStream fs리스트 = new FileStream(리스트파일, System.IO.FileMode.OpenOrCreate, FileAccess.Write, FileShare.Write);
                StreamWriter lst출력 = new StreamWriter(fs리스트, Encoding.GetEncoding(949));


                float y = MMtoPoint(3.0);

                var 완료고객번호 = new ConcurrentBag<string>();
                var 오류고객번호 = new ConcurrentBag<string>();
                
                for (int i = 0; i < 레코드목록.Count; i += batchSize)
                {
                    var batch = 레코드목록.Skip(i).Take(batchSize).ToList();
                    if (batch.Count == 0) continue;

                    int 출력위치 = 0;
                    파일단위++;

                    string 우편묶음파일명 = string.Format("{0}({1:MMdd}){2}_{3}_POST_{4:000}{5}.pdf", 우편출력경로, DateTime.Now, 상태.고객사, 장수구간명, 파일단위, 증권만문구);
                    lst종료 += batch.Count;
                    PdfFont 나눔바른고딕 = PdfFontFactory.CreateFont(@"c:\Easy약관Plus\program\Font\NanumBarunGothic.ttf", PdfEncodings.IDENTITY_H);

                    string[] 리스트항목 = new string[201];
                    using (var writer = new PdfWriter(우편묶음파일명))
                    using (var pdfDoc = new PdfDocument(writer))
                    using (var docw = new Document(pdfDoc))
                    {
                        var merger = new PdfMerger(pdfDoc).SetCloseSourceDocuments(false);
                        MINpage = (int)Math.Ceiling((double)batch[0].우편PDF장수 / 2.0);
                        MAXpage = (int)Math.Ceiling((double)batch[batch.Count - 1].우편PDF장수 / 2.0);
                        
                        docw.SetFont(나눔바른고딕);
                        docw.SetFontSize(7);
                        docw.SetFontColor(ColorConstants.BLACK);
                        //폰트를 공유하면 아래와 같은 오류 발생 
                        //Pdf indirect object belongs to other PDF document. Copy object to current pdf document.

                        
                        foreach (var item in batch)
                        {
                            if( item.우편출력파일 == "") // 우편인데 우편출력파일이 존재하지 않으면 오류(약관생성이나 PDF병합에서 오류가 생기면 파일이 존재하지 않는다)
                            {
                                오류고객번호.Add(item.고객번호);
                                continue;
                            }

                            try
                            {
                                using (PdfReader reader = new PdfReader(item.우편출력파일))
                                using (PdfDocument srcDoc = new PdfDocument(reader))
                                {
                                    int 페이지수 = item.우편PDF장수;
                                    merger.Merge(srcDoc, 1, 페이지수);

                                    종료page += 페이지수;
                                    그룹page++;
                                    출력면수 += 페이지수;
                                    장수 += (int)Math.Ceiling((double)페이지수 / 2.0);
                                    int 소그룹page = 0;

                                    for (int j = 시작page; j <= 종료page; j++)
                                    {
                                        출력위치++;
                                        소그룹page++;
                                        RollCnt++;
                                        string 일련번호 = string.Format("No.{0}   ({1} / {2})", 그룹page, 소그룹page, 페이지수);
                                        docw.ShowTextAligned(new Paragraph(일련번호), MMtoPoint(160), y, 출력위치, TextAlignment.RIGHT, VerticalAlignment.BOTTOM, 0);
                                        docw.ShowTextAligned(new Paragraph(string.Format("{0:000000}", RollCnt)), MMtoPoint(200), y, 출력위치, TextAlignment.RIGHT, VerticalAlignment.BOTTOM, 0);
                                    }
                                    시작page = 종료page + 1;
                                }
                                완료고객번호.Add(item.고객번호);
                            }
                            catch (Exception)
                            {
                                오류고객번호.Add(item.고객번호);
                            }
                            
                        }

                        리스트항목[2] = "보험증권";                          // 세부항목명           
                        리스트항목[3] = 레코드목록[i].상품코드;              // 세부항목코드         
                        리스트항목[4] = "일반";                                  // 발송형태
                        리스트항목[5] = new System.IO.FileInfo(우편묶음파일명).Name; // 파일명  
                        리스트항목[6] = "0";                                 // 구성P                          
                        리스트항목[7] = lst시작.ToString();                  // 시작일련번호        
                        리스트항목[8] = lst종료.ToString();                  // 종료일련번호
                        리스트항목[9] = batch[batch.Count - 1].우편번호;     // 우편번호           
                        리스트항목[10] = "";                                 // 고객명              
                        리스트항목[11] = batch.Count.ToString();             // 건수          
                        리스트항목[12] = 출력면수.ToString();                // 출력면수          
                        //리스트항목[13] = string.Format("구성장수 : {0:0000} ~ {1:0000}", MINpage, MAXpage);  // 전달사항
                        리스트항목[13] = 장수구간명;  // 전달사항

                        리스트항목[16] = 장수.ToString();                    // 폼지1장수   
                        lst시작 = lst종료 + 1;


                        if (await 실행(() => 라벨출력(리스트항목, pdfDoc, docw, 상태), "라벨출력") != "성공")
                        {
                            throw new Exception("라벨출력 오류, 로그참조");
                        }

                        if (await 실행(() => BatchUpdateAsync(상태, 완료고객번호, 오류고객번호, "우편"), "우편파일병합(BatchUpdateAsync)") == "실패")
                        {
                            throw new Exception("우편파일병합(BatchUpdateAsync) 오류, 로그참조");
                        }
                    }

                    string 출력 = "";
                    for (int k = 1; k <= 200; k++)
                    {
                        출력 += 리스트항목[k] + '\t';
                    }
                    lst출력.WriteLine(출력); lst출력.Flush();
                }
                lst출력.Close(); fs리스트.Close();
            }
            catch (Exception)
            {
                실행여부 = "실패";
                throw;
            }

            return 실행여부;
        }

        // ==========================================================================================================
        // 07 - 03 라벨출력
        // ==========================================================================================================
        private async Task<string> 라벨출력(string[] 리스트항목, PdfDocument pdfDoc, Document docw, 현재상태 상태)
        {
            string 실행여부 = "성공" ;
            try
            {
                string 라벨경로 = @"c:\Easy약관Plus\program\라벨.pdf";
                FileStream fsLabel = new FileStream(라벨경로, FileMode.Open, FileAccess.Read, FileShare.Read);
                PdfDocument tmp라벨 = new PdfDocument(new PdfReader(fsLabel));

                // 개별증권을 먼저 묶고 라벨을 앞에 추가
                tmp라벨.CopyPagesTo(1, 2, pdfDoc, 1, new PdfPageFormCopier());

                tmp라벨.Close();
                fsLabel.Close();

                int 라벨인수 = 1;

                string 품명 = string.Format("({0:MM/dd}) 보험증권", DateTime.Now);
                string 파일명 = 리스트항목[5] ?? "";

                System.Globalization.CultureInfo 한글 = System.Globalization.CultureInfo.CreateSpecificCulture("ko-KR");

                DateTime 작업일 = DateTime.Now;
                DateTime 발송일;
                string 작업요일 = 작업일.ToString(string.Format("ddd", 한글));
                switch (작업요일)
                {
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
                string 우편번호 = 리스트항목[9] ?? "";
                string 일련번호 = string.Format("{0} ~ {1}", 리스트항목[7], 리스트항목[8]);


                PdfFont 나눔바른고딕 = PdfFontFactory.CreateFont(@"c:\Easy약관Plus\program\Font\NanumBarunGothic.ttf", PdfEncodings.IDENTITY_H);
                docw.SetFont(나눔바른고딕);
                docw.SetFontSize(18);
                docw.SetFontColor(ColorConstants.BLACK);

                string tmp고객사 = "";
                switch (상태.고객사.ToUpper())
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
                        tmp고객사 = "라이나생명";
                        break;
                    case "LT":
                        tmp고객사 = "롯데손해보험";
                        break;
                    case "DB":
                        tmp고객사 = "DB손해보험";
                        break;
                }

                double X위치 = 23.0;
                라벨세부항목출력(docw, X위치, 297 - 180, 라벨인수, tmp고객사);
                라벨세부항목출력(docw, X위치, 297 - 180, 라벨인수, tmp고객사);
                라벨세부항목출력(docw, X위치, 297 - 56, 라벨인수, 품명);
                X위치 += 12.4;
                라벨세부항목출력(docw, X위치, 297 - 56, 라벨인수, 파일명);
                X위치 += 12.4;
                라벨세부항목출력(docw, X위치, 297 - 202, 라벨인수, 한글발송일);
                라벨세부항목출력(docw, X위치, 297 - 101.2, 라벨인수, 발송형태);
                라벨세부항목출력(docw, X위치, 297 - 28, 라벨인수, 구성P);
                X위치 += 12.4;
                라벨세부항목출력(docw, X위치, 297 - 128, 라벨인수, 폼지);
                X위치 += 12.4;
                X위치 += 12.4;
                X위치 += 12.4;
                라벨세부항목출력(docw, X위치, 297 - 128, 라벨인수, 약관);
                X위치 += 12.4;
                라벨세부항목출력(docw, X위치, 297 - 128, 라벨인수, 봉투);
                X위치 += 23.4;
                라벨세부항목출력(docw, X위치, 297 - 128, 라벨인수, 전달사항);
                X위치 += 16.4;
                라벨세부항목출력(docw, X위치, 297 - 128, 라벨인수, OP);
                X위치 += 14.1;
                라벨세부항목출력(docw, X위치, 297 - 128, 라벨인수, IMS);
                X위치 += 14.1;
                라벨세부항목출력(docw, X위치, 297 - 128, 라벨인수, 수작업);
                X위치 += 12.0;
                라벨세부항목출력(docw, X위치, 297 - 217, 라벨인수, 고객명);
                라벨세부항목출력(docw, X위치, 297 - 131.1, 라벨인수, 우편번호);
                라벨세부항목출력(docw, X위치, 297 - 41.2, 라벨인수, 일련번호);

                // 롤페이지cnt;
                int RollCnt = 1;
                docw.ShowTextAligned(new Paragraph(string.Format("{0:000000}", RollCnt)), MMtoPoint(100), MMtoPoint(1.5), 라벨인수, TextAlignment.LEFT, VerticalAlignment.BOTTOM, 0);
                RollCnt += 1;
                docw.ShowTextAligned(new Paragraph(string.Format("{0:000000}", RollCnt)), MMtoPoint(100), MMtoPoint(1.5), 라벨인수 + 1, TextAlignment.LEFT, VerticalAlignment.BOTTOM, 0);
                
            }
            catch (Exception)
            {
                await Task.Delay(TimeSpan.FromSeconds(0.01));
                실행여부 = "실패";
                throw;

            }
            return 실행여부;
        }
     
        private void 라벨세부항목출력(Document docw , double X, double Y, int 위치, string 항목)
        {
            메소드명 = "라벨세부항목출력";
            docw.ShowTextAligned(new Paragraph(항목), MMtoPoint(X), MMtoPoint(Y), 위치, TextAlignment.CENTER, VerticalAlignment.BOTTOM, (float)(Math.PI / 2));
        }

        // ==========================================================================================================
        // 10 - 모든작업이 완료(오류포함)되면 해당 데이터를 삭제/이동한다.
        //      완료 : data처리, data 삭제 ==> Err_data, Err_data처리 에서도 삭제(재작업시 존재함) 
        //      오류 : data처리, data 이동 ==> Err_data, Err_data처리
        // ==========================================================================================================
        private async Task<string> 완료오류처리(현재상태 상태, string 완료오류)
        {
            string 실행여부 = "성공";
            try
            {
                string SqlStr = "";
                string[] tmp테이블 = new string[] { "data", "data처리" };

                foreach (string tmeTabel in tmp테이블)
                {
                    // 공통 변수는 if문 밖으로 빼서 else에서도 쓸 수 있게 정리했습니다.
                    string 컴이름 = SystemInformation.ComputerName.ToLower();
                    bool 오류이동 = NetworkManager.고객사선택("", 컴이름) == "KB2" ? true : false;

                    if (완료오류 == "오류" || 완료오류 == "실패" || 완료오류 == "대기" || 완료오류 == "")
                    {
                        // =========================================================
                        // 1. 기존 에러 처리 로직 (수정 없음)
                        // =========================================================
                        SqlStr = $@"
                    INSERT INTO Err_{tmeTabel}  
                    SELECT *
                    FROM {tmeTabel}
                    WHERE groupKey = '{상태.groupKey}'
                ";

                        if (await 실행(() => DBUpdate(SqlStr, 상태.dataTable접속), "완료오류처리(DBUpdate)01") == "실패")
                        {
                            throw new Exception("완료오류처리(DBUpdate)01 오류, 로그참조");
                        }

                        if (오류이동 && 컴이름 != "dps77" && tmeTabel == "data")
                        {
                            string selectErrorOnlySql = $@"
                        SELECT * FROM {tmeTabel} 
                        WHERE groupKey = '{상태.groupKey}' 
                          AND 모바일작업 NOT IN ('모바일완료', '완료')
                    ";

                            DataTable dtErrorOnly = new DataTable();

                            using (SqlConnection srcConn = new SqlConnection(상태.dataTable접속))
                            {
                                await srcConn.OpenAsync();
                                using (SqlCommand srcCmd = new SqlCommand(selectErrorOnlySql, srcConn))
                                {
                                    using (SqlDataReader reader = await srcCmd.ExecuteReaderAsync())
                                    {
                                        dtErrorOnly.Load(reader);
                                    }
                                }
                            }

                            if (dtErrorOnly != null && dtErrorOnly.Rows.Count > 0)
                            {
                                using (SqlConnection destConn = new SqlConnection(상태.URLTable접속))
                                {
                                    await destConn.OpenAsync();

                                    string updateSql = @"
                                        UPDATE MobileTermsMain
                                        SET ProcessState = 'E3_ERR',     
                                            ErrorMessage = '약관생성오류', 
                                            UpdatedAt = GETDATE()
                                        WHERE PrimaryKey = @고객Key;
                                    ";

                                    using (SqlCommand updateCmd = new SqlCommand(updateSql, destConn))
                                    {
                                        updateCmd.Parameters.Add("@고객Key", SqlDbType.VarChar);

                                        foreach (DataRow row in dtErrorOnly.Rows)
                                        {
                                            if (row["고객Key"] != DBNull.Value)
                                            {
                                                try
                                                {
                                                    updateCmd.Parameters["@고객Key"].Value = row["고객Key"].ToString();
                                                    await updateCmd.ExecuteNonQueryAsync();
                                                }
                                                catch (Exception ex)
                                                {
                                                    로그기록($"{DateTime.Now:yy.MM.dd HH:mm:ss} [경고] 모바일 상태 업데이트 실패 (고객Key: {row["고객Key"]}) : {ex.Message}", 1);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        // =========================================================
                        // 2. 정상 완료 시 과거 에러 상태 복구(치유) 로직
                        // =========================================================
                        if (오류이동 && 컴이름 != "dps77" && tmeTabel == "data")
                        {
                            // 현재 groupKey에 해당하는 고객Key 목록만 조회
                            string selectSuccessSql = $@"
                                SELECT 고객Key FROM {tmeTabel} 
                                WHERE groupKey = '{상태.groupKey}'
                            ";

                            DataTable dtSuccess = new DataTable();

                            using (SqlConnection srcConn = new SqlConnection(상태.dataTable접속))
                            {
                                await srcConn.OpenAsync();
                                using (SqlCommand srcCmd = new SqlCommand(selectSuccessSql, srcConn))
                                {
                                    using (SqlDataReader reader = await srcCmd.ExecuteReaderAsync())
                                    {
                                        dtSuccess.Load(reader);
                                    }
                                }
                            }

                            if (dtSuccess != null && dtSuccess.Rows.Count > 0)
                            {
                                using (SqlConnection destConn = new SqlConnection(상태.URLTable접속))
                                {
                                    await destConn.OpenAsync();

                                    // 💡 핵심: ProcessState가 'E3_ERR' 인 것만 타겟팅하여 무의미한 DB 쓰기 부하를 방지합니다.
                                    string healSql = @"
                                        UPDATE MobileTermsMain
                                        SET ProcessState = '21_PROC_DONE',     
                                            ErrorMessage = '', 
                                            UpdatedAt = GETDATE()
                                        WHERE PrimaryKey = @고객Key
                                          AND ProcessState = 'E3_ERR'; 
                                    ";

                                    using (SqlCommand healCmd = new SqlCommand(healSql, destConn))
                                    {
                                        healCmd.Parameters.Add("@고객Key", SqlDbType.VarChar);

                                        foreach (DataRow row in dtSuccess.Rows)
                                        {
                                            if (row["고객Key"] != DBNull.Value)
                                            {
                                                try
                                                {
                                                    healCmd.Parameters["@고객Key"].Value = row["고객Key"].ToString();

                                                    // 조건(E3_ERR)에 맞지 않는 99%의 정상 데이터는 
                                                    // 여기서 디스크 I/O 없이 0 Rows Affected로 초고속 통과됩니다.
                                                    await healCmd.ExecuteNonQueryAsync();
                                                }
                                                catch (Exception ex)
                                                {
                                                    로그기록($"{DateTime.Now:yy.MM.dd HH:mm:ss} [경고] 모바일 상태 복구 실패 (고객Key: {row["고객Key"]}) : {ex.Message}", 1);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }

                    // =========================================================
                    // 3. 작업 완료 후 Data 테이블에서 삭제 (공통 로직)
                    // =========================================================
                    SqlStr = $@"
                        DELETE FROM {tmeTabel}
                        WHERE groupKey = '{상태.groupKey}'; 
                    ";

                    if (await 실행(() => DBUpdate(SqlStr, 상태.dataTable접속), "완료오류처리(DBUpdate)02") == "실패")
                    {
                        throw new Exception("완료오류처리(DBUpdate)02 오류, 로그참조");
                    }
                }

                if (await 실행(() => 오류작업View(상태), "오류작업View") == "실패")
                {
                    throw new Exception("오류작업View 오류, 로그참조");
                }
            }
            catch (Exception)
            {
                실행여부 = "실패";
                throw;
            }
            return 실행여부;
        }

        private async Task<string> 오류작업View (현재상태 상태)
        {
            string 실행여부 = "성공";
            int 재시도 = 0;
            while (true)
            {
                try
                { 
                    using (SqlConnection AdoNet = new SqlConnection(상태.dataTable접속))
                    {
                        await AdoNet.OpenAsync();

                        DataSet ds = new DataSet();
                        DataTable dataTable = new DataTable();
                        string SqlStr = $@"
                            SELECT groupKey, 처리구분, 고객사, 상태, 전체건수, 처리단계
                            FROM Err_data처리
                                WHERE 처리구분 = '00'
                            ORDER BY 시작시간
                            ";
                        SqlCommand sqlcmd = new SqlCommand(SqlStr, AdoNet);
                        SqlDataAdapter adapter = new SqlDataAdapter(sqlcmd);
                        adapter.Fill(ds, "기본");

                        DataColumn 확인01 = new DataColumn("확인", typeof(bool)); // 확인 필드추가
                        확인01.DefaultValue = false;
                        ds.Tables["기본"].Columns.Add(확인01);
                        확인01.SetOrdinal(0);

                        SqlStr = $@"
                            SELECT groupKey, 처리구분, 고객사, 상태, 전체건수, 처리단계
                            FROM Err_data처리
                                WHERE 처리구분 <> '00'
                            ORDER BY 시작시간
                               ";

                        sqlcmd = new SqlCommand(SqlStr, AdoNet);
                        adapter = new SqlDataAdapter(sqlcmd);
                        adapter.Fill(ds, "상세");

                        DataColumn KeyColumn = ds.Tables["기본"].Columns["groupKey"];
                        DataColumn ForeignKeyColumn = ds.Tables["상세"].Columns["groupKey"];

                        ds.Relations.Add("상세", KeyColumn, ForeignKeyColumn);

                        grd오류작업리스트.DataSource = ds.Tables[0];
                        grd오류작업리스트.ForceInitialize();

                        GridView GDOPView = new GridView(grd오류작업리스트);
                        grd오류작업리스트.LevelTree.Nodes.Add("상세", GDOPView);
                        GDOPView.ViewCaption = "서브컴";
                        GDOPView.GroupPanelText = "서브컴 상세 리스트";
                        view오류작업리스트.ExpandMasterRow(0); //0:해당라인, 펼치기

                        view오류작업리스트.Appearance.Row.Font = new Font("카이겐고딕 KR Regular", 9);

                        if (await 실행(() => grdListFile_View(1), "grdListFile_View") == "실패")
                            throw new Exception("ER00_grdListFile_View 오류, 로그참조");

                    }
                    break;
                }
                catch (Exception)
                {
                    재시도++;
                    if (재시도 <= 3) {
                        await Task.Delay(TimeSpan.FromSeconds(5));
                        continue;
                    }
                    실행여부 = "실패";
                    throw;
                }
            }
            return 실행여부;
        }

        private void view오류작업리스트_PopupMenuShowing(object sender, PopupMenuShowingEventArgs e)
        {
            if (e.MenuType == GridMenuType.Row) // 행(Row)에서만 메뉴 보이기
            {
                e.Menu.Items.Clear(); // 기본 메뉴 제거
                int rowHandle = e.HitInfo.RowHandle;

                // 수정 메뉴
                DXMenuItem editItem = new DXMenuItem(" 재 작 업", (o, args) =>
                {
                    //var data = view오류작업리스트.GetRow(rowHandle);
                    string  tmpgroupKey = view오류작업리스트.GetRowCellValue(rowHandle, "groupKey").ToString();
                    
                    상태.groupKey = tmpgroupKey;
                    _ = 재작업설정(상태, "재작업");

                });
                editItem.ImageOptions.Image = Image.FromFile(string.Format("{0}\\img\\grd_재작업.png", Application.StartupPath));

                // 삭제 메뉴
                DXMenuItem deleteItem = new DXMenuItem(" 삭    제", (o, args) =>
                {
                    // 1. 현재 편집 중인 체크박스 상태를 GridView 및 DataSource에 확실히 반영
                    view오류작업리스트.PostEditor();
                    view오류작업리스트.UpdateCurrentRow();

                    // 2. 체크된 항목들의 groupKey 수집
                    List<string> targetGroupKeys = new List<string>();
                    for (int i = 0; i < view오류작업리스트.RowCount; i++)
                    {
                        object isCheckedObj = view오류작업리스트.GetRowCellValue(i, "확인");
                        bool isChecked = isCheckedObj != null && isCheckedObj != DBNull.Value && Convert.ToBoolean(isCheckedObj);

                        if (isChecked)
                        {
                            string tmpgroupKey = view오류작업리스트.GetRowCellValue(i, "groupKey").ToString();
                            targetGroupKeys.Add(tmpgroupKey);
                        }
                    }

                    // 3. 체크된 항목이 없는 경우 -> 우클릭한 단일 행(포커스된 행)을 타겟으로 설정
                    if (targetGroupKeys.Count == 0)
                    {
                        // rowHandle은 팝업 메뉴를 띄울 때 클릭한 행의 인덱스입니다.
                        object focusedKeyObj = view오류작업리스트.GetRowCellValue(rowHandle, "groupKey");
                        if (focusedKeyObj != null && focusedKeyObj != DBNull.Value)
                        {
                            targetGroupKeys.Add(focusedKeyObj.ToString());
                        }
                        else
                        {
                            return; // 빈 그리드 빈 공간 우클릭 등 예외 상황 방어
                        }
                    }

                    // 4. 삭제 확인 메시지 (단건/다건 구분)
                    string message = targetGroupKeys.Count > 1
                        ? $"선택된 {targetGroupKeys.Count}개의 항목을 정말 삭제하시겠습니까?"
                        : "정말 삭제하시겠습니까?";

                    if (MessageBox.Show(message, "확인", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    {
                        foreach (string key in targetGroupKeys)
                        {
                            상태.groupKey = key;
                            _ = 재작업설정(상태, "삭제");
                        }
                    }
                });
                deleteItem.ImageOptions.Image = Image.FromFile(string.Format("{0}\\img\\grd_삭제.png", Application.StartupPath));

                // 새로고침 메뉴
                DXMenuItem refreshItem = new DXMenuItem(" 새로고침", (o, args) =>
                {
                    view오류작업리스트.RefreshData();
                });
                refreshItem.ImageOptions.Image = Image.FromFile(string.Format("{0}\\img\\grd_새로고침.png", Application.StartupPath));

                // 메뉴에 추가
                e.Menu.Items.Add(editItem);
                e.Menu.Items.Add(deleteItem);
                e.Menu.Items.Add(refreshItem);
            }

        }

        private async Task 재작업설정(현재상태 상태, string 조건)
        {
            메소드명 = "재작업설정";
            try
            {
                string SqlStr = "";
                List<string> 오류실행 = new List<string>();
                
                if (조건 == "재작업")
                {
                    SqlStr = $@"
                        UPDATE Err_data처리
                        SET 상태 = '처리', 실행횟수 = 0
                        OUTPUT INSERTED.* INTO data처리
                        WHERE groupKey = '{상태.groupKey}';";
                    오류실행.Add(SqlStr);

                    SqlStr = $@"
                        UPDATE Err_data
                        SET 상태 = '처리'
                        OUTPUT INSERTED.* INTO data
                        WHERE groupKey = '{상태.groupKey}';";
                    오류실행.Add(SqlStr);
                }

                //삭제는 공통(재작업 값에도 이동후 삭제)
                SqlStr = $@"
                    DELETE FROM Err_data처리         
                    WHERE groupKey = '{상태.groupKey}'
                        ";
                오류실행.Add(SqlStr);
                SqlStr = $@"
                    DELETE FROM Err_data         
                    WHERE groupKey = '{상태.groupKey}'
                        ";
                오류실행.Add(SqlStr);

                foreach (string 실행쿼리 in 오류실행)
                {
                    _ = await DBUpdate(실행쿼리, 상태.dataTable접속);
                }

                _ = await 실행(() => 오류작업View(상태), "오류작업View");

                if (조건 == "재작업")
                {
                    string 재작업파일 = $"{저장.기본파일폴더}@{상태.groupKey}";
                    string 출력내용 = $"{상태.고객사}|{상태.groupKey}|{상태.출력폴더}";
                    File.WriteAllText(재작업파일, 출력내용, Encoding.GetEncoding(949));
                }
            }
            catch (Exception)
            {
                throw;
            }
            
        }



        //========================================================================================================================================
        // **************** 공통 **************** 
        //========================================================================================================================================

        // =============================================================================
        //  캡슐화 비동기식(공통)
        // =============================================================================
        // Task<bool> 과 Func<Task<bool>> 같은 타입 즉 Func<Task<bool>>는 메소드의 반환값, 이걸 받아서 다시 처음 호출한 곳으로 반환한다.
        // 아래 동기식도 동일
        private async Task<string> 실행(Func<Task<string>> func, string name)
        {
            return await NetworkManager.실행(func, name, (msg, level) =>
            {
                로그기록(msg, level);
                진행오류메세지출력();
            });
        }

        private async Task<(string, string)> 실행(Func<Task<(string, string)>> func, string name)
        {
            (string 실행여부, string 오류메시지) 결과;

            try
            {
                // 튜플 결과를 그대로 받음
                결과 = await func();
            }
            catch (Exception e1)
            {
                // 에러 발생 시 처리 (기존 로직 복사)
                에러 = new System.Diagnostics.StackTrace(e1, true);
                에러코드출력(에러, out int 라인);
                로그내용 = string.Format("{0:yy.MM.dd HH:mm:ss} [오류] {1} (메소드 : {2}, Line : {3})", DateTime.Now, e1.Message, name, 라인);
                로그기록(로그내용, 1);
                진행오류메세지출력();

                // [중요] 실패 시에도 튜플 형식인 (string, string)으로 리턴해야 함
                // 첫 번째 값은 "실패", 두 번째 값은 에러 메시지를 담아 리턴
                결과 = ("실패", e1.Message);
            }

            return 결과;
        }


        // =============================================================================
        //  캡슐화 동기식(공통)
        // =============================================================================
        private async Task<bool> 실행(Func<bool> func, string name)
        {
            bool 실행여부 = true;
            try
            {
                실행여부 = await Task.Run(func); // 동기 작업을 Task로 실행
            }
            catch (Exception e1)
            {
                실행여부 = false;
                에러 = new System.Diagnostics.StackTrace(e1, true);
                에러코드출력(에러, out int 라인);
                로그내용 = string.Format("{0:yy.MM.dd HH:mm:ss} [오류] {1} (메소드 : {2}, Line : {3})",
                                         DateTime.Now, e1.Message, name, 라인);
                로그기록(로그내용, 1);
            }
            return 실행여부;
        }

        // =============================================================================
        //  작업순서이미지(공통)
        // =============================================================================
        private PictureBox[] pic순서;


        private async Task<string> 작업순서이미지설정()
        {
            string 실행여부 = "성공";
            try
            {
                int 최대단계 = 12; // 1부터 시작
                pic순서 = new PictureBox[최대단계];
                lab순서 = new Label[최대단계]; // 올바르게 수정된 부분

                foreach (System.Windows.Forms.Control control in grd작업순서.Controls)
                {
                    string 이름 = control.Name;
                    if (이름.StartsWith("pic순서"))
                    {
                        // 이름뒷 2자리는 숫자로 되어 있다
                        TmpControl(이름, out int 인수);
                        pic순서[인수] = control as PictureBox;
                    }
                    else if (control.Name.StartsWith("lab순서"))
                    {
                        TmpControl(이름, out int 인수);
                        lab순서[인수] = control as Label;

                    }
                }
                for (int i = 1; i < 최대단계; i++)
                    _ = await 이미지읽기(i, "진행전");
            }
            catch (Exception)
            {
                실행여부 = "실패";
                throw;
            }
            return 실행여부;
        }

        private void TmpControl(string _controlName, out int 인수)
        {
            int 길이 = -1;
            길이 = _controlName.Length;
            인수 = Convert.ToInt16(_controlName.Substring(길이 - 2, 2));  // 이름뒷 2자리는 숫자로 되어 있다
        }
        private async Task<string> 이미지읽기(int 인수, string 대상)
        {
            await Task.Delay(100);
            return await Task.Run(() =>
            {
                string 실행여부 = "성공";
                try
                {
                    switch (대상)
                    {
                        case "진행전":
                            pic순서[인수].Image = Image.FromFile(string.Format("{0}\\img\\진행전_{1:00}.png", Application.StartupPath, 인수)); //진행전_01.png ~ 진행전_11.png
                            lab순서[인수].ForeColor = System.Drawing.Color.Transparent;
                            lab순서[인수].BackColor = System.Drawing.Color.Transparent;
                            break;
                        case "진행후":
                            pic순서[인수].Image = Image.FromFile(string.Format("{0}\\img\\진행후_{1:00}.png", Application.StartupPath, 인수)); //진행후_01.png ~ 진행후_11.png

                            for (int i = 1; i < lab순서.Length; i++)
                            {
                                lab순서[i].ForeColor = System.Drawing.Color.Transparent;
                                lab순서[i].BackColor = System.Drawing.Color.Transparent;
                            }
                            lab순서[인수].ForeColor = System.Drawing.Color.White;
                            lab순서[인수].BackColor = System.Drawing.Color.Orange;
                            break;
                    }
                    
                }
                catch (Exception)
                {
                    실행여부 = "실패";
                }
                return 실행여부;

            });
        }

       

        // "2025.12" 형식의 문자열을 파싱하는 함수
        static bool ParseYearMonth(string folderName, out int year, out int month)
        {
            year = 0;
            month = 0;

            string[] parts = folderName.Split('.');
            if (parts.Length != 2) return false;

            if (int.TryParse(parts[0], out year) && int.TryParse(parts[1], out month))
            {
                return true;
            }
            return false;
        }

        // =============================================================================
        //  로그출력(공통)
        // =============================================================================

        // =============================================================================
        //  로그파일생성(공통)
        // =============================================================================
        // ★ 하이브리드 고속 로깅: 외부 타이머 없이 스스로 날짜를 체크하고 갱신합니다.
        private bool 로그파일열기(DateTime now)
        {
            bool 실행여부 = true;

            try
            {
                // 1. 기존에 열린 파일이 있다면 안전하게 닫고 메모리를 해제합니다.
                if (Logwr != null)
                {
                    try { Logwr.Flush(); Logwr.Close(); Logwr.Dispose(); } catch { }
                    Logwr = null;
                }

                // 2. 로그 폴더 동적 생성 (c:\Easy약관Plus\program\LogFile\yyyy.MM\dd\)
                // (기존 '저장.로그경로' 대신 하드코딩된 안정적인 경로를 쓰시거나, 기존 변수를 유지하셔도 됩니다)
                string 로그폴더 = System.IO.Path.Combine(@"c:\Easy약관Plus\program\LogFile\", now.ToString("yyyy.MM"), now.ToString("dd"));
                if (!Directory.Exists(로그폴더)) Directory.CreateDirectory(로그폴더);

                // 3. 파일 이름 결정 (예: (260503)맞춤약관로그.log)
                string 로그파일 = System.IO.Path.Combine(로그폴더, $"({now:yyMMdd})맞춤약관로그.log");

                // 4. 스트림 열기 (FileShare.ReadWrite로 다른 프로그램에서 실시간 읽기 허용)
                FileStream fs = new FileStream(로그파일, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
                Logwr = new StreamWriter(fs, Encoding.GetEncoding(949));

                // ★ 핵심: 프로그램이 크래시 나더라도 버퍼에 있는 로그를 즉시 디스크에 씁니다.
                Logwr.AutoFlush = true;

                // 5. 현재 열려있는 로그 파일의 기준 '일(Day)'을 저장합니다.
                _currentLogDay = now.Day;
               
            }
            catch (Exception ex)
            {
                MessageBox.Show($"🚫진행불가 : 로그파일 설정에 실패 했습니다.\n\n{ex.Message}", "로그파일", MessageBoxButtons.OK);
                실행여부 = false;
            }

            return 실행여부;
        }

        // ★ 하이브리드 고속 로깅: 호출될 때마다 날짜를 체크하여 스스로 파이프를 교체함
        private void 로그기록(string 내용, int 로그인수)
        {
            try
            {
                int 일치1 = -1, 일치2 = -1;

                // =========================================================================
                // [1] 파일 기록 파트 (인수가 1일 때만 파일에 기록)
                // =========================================================================
                if (로그인수 == 1)
                {
                    DateTime now = DateTime.Now;

                    // ★ 고속 파일 로깅: 날짜가 바뀌었거나 스트림이 없으면 스스로 새로 엽니다.
                    if (Logwr == null || _currentLogDay != now.Day)
                    {
                        로그파일열기(now); // 이전 답변에서 수정한 로그파일열기 메서드 호출
                    }

                    try
                    {
                        if (Logwr != null)
                        {
                            Logwr.WriteLine(내용);
                            Logwr.Flush(); // 원본 유지
                        }
                    }
                    catch
                    {
                        // 외부에서 폴더/파일이 삭제되었을 경우를 대비한 셀프 힐링
                        try { 로그파일열기(now); Logwr.WriteLine(내용); Logwr.Flush(); } catch { }
                    }
                }
                // =========================================================================
                // [2] 핫폴더 감시 문구 체크 파트 (인수가 0일 때 등 파일에 쓰지 않는 경우)
                // =========================================================================
                else
                {
                    if (lst로그.Items.Count > 0)
                    {
                        // 대표님의 원본 로직 완벽 복원 (대기 문구 IndexOf 체크)
                        string 대기문구 = "[핫폴더 감시] INPUT / LIST 폴더 감시 중 : WAITING";
                        일치1 = 내용.IndexOf(대기문구);
                        일치2 = lst로그.Items[0].ToString().IndexOf(대기문구);
                    }
                }

                // =========================================================================
                // [3] UI 화면 출력 파트 (대표님 원본 그대로 유지)
                // =========================================================================
                if (!this.IsHandleCreated)
                {
                    this.HandleCreated += (s, e) => 로그기록(내용, 로그인수);
                    return;
                }

                this.BeginInvoke(new Action(() =>
                {
                    // 300개 제한
                    if (lst로그.Items.Count > 300) { lst로그.Items.RemoveAt(300); }

                    // 대기 문구 연속일 경우 0번 인덱스 덮어쓰기 (화면 밀림 방지)
                    if (일치1 != -1 && 일치2 != -1)
                        lst로그.Items[0] = 내용;
                    else
                        lst로그.Items.Insert(0, 내용); // 최신 로그는 무조건 맨 위로 인서트

                    // 항상 최상단 포커스 유지
                    lst로그.SelectedIndex = 0;
                }));
            }
            catch (Exception) { }
        }

        // =============================================================================
        //  쿼리파일생성(공통)
        // =============================================================================
        private void 쿼리파일열기(string 쿼리파일)
        {
            try
            {
                Sqlfs = new FileStream(쿼리파일, FileMode.Append, FileAccess.Write, FileShare.Write);
                Sqlwr = new StreamWriter(Sqlfs, Encoding.GetEncoding(949));
            }
            catch (Exception)
            {
            }
        }
       
        private void 쿼리파일닫기()
        {
            Sqlwr.Close();
            Sqlfs.Close(); Sqlfs.Dispose();
        }

        private void 쿼리기록(string 내용)
        {
            try
            {
                Sqlwr.WriteLine(내용); Sqlwr.Flush();
            }
            catch (Exception) {}
        }

        private void lst로그_MouseClick(object sender, MouseEventArgs e)
        {
            if (lst로그.Items.Count < 1)
            {
                return;
            }
            string 문구 = lst로그.Items[lst로그.SelectedIndex].ToString();
            view작업리스트.GroupPanelText = 문구;
            Clipboard.SetText(문구);

        }
                
        // =============================================================================
        //  복사, 이동, 삭제(공통)
        // =============================================================================
        private bool 복사_이동_삭제(string 명령, string 원본경로, string 대상경로)
        {
            메소드명 = "복사_이동_삭제";
            bool 실행여부 = true;
            try
            {
                switch (명령)
                {
                    case "복사_삭제":
                        new FileInfo(원본경로).CopyTo(대상경로, true);
                        new FileInfo(원본경로).Delete();
                        break;
                }
            }
            catch (Exception) {
                실행여부 = false;
            }
            return 실행여부;
        }

        // =============================================================================
        //  폴더생성(공통)
        // =============================================================================
        private string 폴더생성(string 현재폴더, string 하위폴더)
        {
            메소드명 = "폴더생성";
            string 하위폴더확인 = string.Format("{0}{1}\\", 현재폴더, 하위폴더);
            if (new System.IO.DirectoryInfo(하위폴더확인).Exists == false)
                new System.IO.DirectoryInfo(하위폴더확인).Create();
            return 하위폴더확인;
        }

        // =============================================================================
        //  폴더생성상세(공통)
        // =============================================================================
        private async Task<string> 폴더생성상세(현재상태 상태)
        {
            string 실행여부 = "성공";
            try
            {
                // ex) 출력폴더\\고객사(KB)\년월(yyyy.MM)\일(dd)\시분초(HHmmss) ==> ex) d:\Easy약관Plus\Output\KB\2021.02\19\182855\XXXXX.pdf
                // 출력에 필요한 기본 폴더 생성
                // 출력폴더가 없으면 먼저생성 후 '@완료' 폴더가 생성된다.
                string 처리완료 = 폴더생성(상태.출력폴더, "@완료"); // 개별로 되어 있는 약관파일(병합하기전) 이동
                처리완료 = 폴더생성(상태.출력폴더, "Y");
                처리완료 = 폴더생성(상태.출력폴더, "상품별세션");
                처리완료 = 폴더생성(상태.출력폴더, "목차");
                처리완료 = 폴더생성(상태.출력폴더, "StateFile");   //F1 ~ F8앱에서 PDF변환 완료 후 *.End 생성
                처리완료 = 폴더생성(저장.기본출력폴더, "감시");
                for (int i = 1; i <= Convert.ToInt32(cob스레드수.EditValue); i++)
                {
                    string 분할 = 폴더생성(상태.출력폴더, i.ToString());
                    string 완료 = 폴더생성(분할, "@완료");
                
                }
            }
            catch (Exception)
            {
                실행여부 = "실패";
                throw;
            }
            await Task.CompletedTask;
            return 실행여부;
        }

        // =============================================================================
        //  데이터 UPDATE/DELETE 상태(공통)
        // =============================================================================
        private async Task<string> DBUpdate(string SqlStr, string dataTable접속, params SqlParameter[] parameters)
        {
            return await NetworkManager.DBUpdate(SqlStr, dataTable접속, parameters);
        }

        // =============================================================================
        //  단계진행 출력문구 출력
        // =============================================================================
        private void 단계진행문구출력(string 단계)
        {
            메소드명 = "단계진행문구출력";
            switch (단계)
            {
                case "00":
                    로그내용 = string.Format("{0:yy.MM.dd HH:mm:ss} [처리단계00]H2PControl App 시작 단계 : F1 ~ F8 PDF변환 시작.", DateTime.Now);
                    로그기록(로그내용, 1);
                    break;
                case "03":
                    로그내용 = string.Format("{0:yy.MM.dd HH:mm:ss} [처리단계03]개별약관 생성 단계 : 개인별 맞춤약관 생성 시작.", DateTime.Now);
                    로그기록(로그내용, 1);
                    break;
                case "06":
                    로그내용 = string.Format("{0:yy.MM.dd HH:mm:ss} [처리단계06]증권 + 약관 단계 : 증권,약관 결합 시작", DateTime.Now);
                    로그기록(로그내용, 1);
                    로그내용 = string.Format("{0:yy.MM.dd HH:mm:ss} [처리단계06]증권만,약관만 분류 단계 : 증권, 약관분리", DateTime.Now);
                    로그기록(로그내용, 1);
                    로그내용 = string.Format("{0:yy.MM.dd HH:mm:ss} [처리단계06-1]북마크 생성 단계 : 북마크생성", DateTime.Now);
                    로그기록(로그내용, 1);
                    로그내용 = string.Format("{0:yy.MM.dd HH:mm:ss} [처리단계06-2]P, M, E 분류 단계 : P(우편),M(모바일),E(이메일) 분류완료", DateTime.Now);
                    로그기록(로그내용, 1);
                    break;
                case "07-A":
                    로그내용 = string.Format("{0:yy.MM.dd HH:mm:ss} [처리단계07A]우편파일병합 단계 : 우편출력파일을 병합 시작", DateTime.Now);
                    로그기록(로그내용, 1);
                    break;
                case "07-B":
                    로그내용 = string.Format("{0:yy.MM.dd HH:mm:ss} [처리단계07B]FTP FILE UPLOAD 단계 : FTP 에 File을 Upload 시작", DateTime.Now);
                    로그기록(로그내용, 1);
                    break;
            }
            Application.DoEvents();
        }

        // =============================================================================
        //  PDF파일 패스워드 설정
        // =============================================================================
        private void setPW(out WriterProperties setPassword, string userPass, string owerPass)
        {
            메소드명 = "setPW";
            setPassword = null;

            byte[] byteuserPass = Encoding.GetEncoding(949).GetBytes(userPass);
            byte[] byteowerPass = Encoding.GetEncoding(949).GetBytes(owerPass);

            setPassword = new WriterProperties().SetStandardEncryption(byteuserPass, byteowerPass, EncryptionConstants.ALLOW_PRINTING,
                                 EncryptionConstants.ENCRYPTION_AES_256 | EncryptionConstants.DO_NOT_ENCRYPT_METADATA);

        }

        // =============================================================================
        //  MM to Point
        // =============================================================================
        private float MMtoPoint(double x)
        {
            float tmpx;
            tmpx = (float)(x * 2.83465);
            return tmpx;
        }

        // =============================================================================
        //  그리드 화면설정(공통)
        // =============================================================================
        private async Task< DataTable> 작업리스트DT1()
        {
            return await Task.Run(() =>
            {
                var table = new DataTable("작업리스트");

                // 컬럼 정의를 배열로 묶기
                (string name, Type type)[] columns =
                {
                    ("확인", typeof(bool)),
                    ("groupKey", typeof(string)),
                    ("고객사", typeof(string)),
                    ("상태", typeof(string)),
                    ("시작시간", typeof(string)),
                    ("전체건수", typeof(string)),
                    ("병렬컴", typeof(string)),
                    ("처리단위", typeof(string)),
                    ("처리단계", typeof(string)),
                    ("실행횟수", typeof(int)),
                    ("약관작업", typeof(string)),
                    ("PDF변환작업", typeof(string)),
                    ("PDF병합작업", typeof(string)),
                    ("우편작업", typeof(string)),
                    ("FTP작업", typeof(string)),
                    ("모바일작업", typeof(string)),
                    ("앱실행수", typeof(int))
                };

                foreach (var (name, type) in columns)
                    table.Columns.Add(name, type);

                return table;
            });
        }
 
        private async Task<string> grdListFile_View(int 인수)
        {
            string 실행여부 = "성공";
            try
            {
                // 1. 비동기 데이터 로드 (UI 멈춤 방지)
                if (인수 == 0)
                {
                    DataTable table = await Task.Run(() => 작업리스트DT1());
                    grd작업리스트.DataSource = table;
                }

                // [최적화 1] 불필요한 객체 생성(new GridView()) 제거 및 메모리 낭비 방지
                GridView gdview = view작업리스트;

                // [최적화 2] 렌더링 최적화 시작 및 먹통 방지용 try~finally 적용
                gdview.BeginUpdate();
                try
                {
                    // --- 기본 설정 ---
                    gdview.GroupPanelText = "항목을 그룹화 합니다.";
                    gdview.Appearance.GroupPanel.Font = new Font("카이겐고딕 KR Regular", 10);
                    gdview.Appearance.GroupPanel.ForeColor = System.Drawing.Color.DarkBlue;
                    gdview.OptionsView.ShowIndicator = false; // 인디게이터 숨김

                    gdview.Appearance.HeaderPanel.Font = new Font("카이겐고딕 KR Regular", 10);
                    gdview.Appearance.HeaderPanel.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
                    gdview.Appearance.HeaderPanel.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;

                    gdview.OptionsSelection.MultiSelectMode = GridMultiSelectMode.CellSelect;  // 멀티라인 선택
                    gdview.OptionsSelection.MultiSelect = true;
                    gdview.Appearance.SelectedRow.BackColor = System.Drawing.Color.White;
                    gdview.Appearance.SelectedRow.ForeColor = System.Drawing.Color.Blue;

                    gdview.ColumnPanelRowHeight = 25;       // 제목필드 크기 
                    gdview.OptionsBehavior.EditorShowMode = DevExpress.Utils.EditorShowMode.Click; //원클릭

                    // [최적화 3] 컬럼 너비 설정 (튜플 배열 활용으로 코드 15줄 압축)
                    int grdwidth = grd작업리스트.Width;
                    var columnWidths = new (string Name, double Ratio)[]
                    {
                        ("확인", 0.06), ("groupKey", 0.076), ("고객사", 0.076), ("상태", 0.076),
                        ("시작시간", 0.092), ("전체건수", 0.076), ("병렬컴", 0.076), ("처리단위", 0.076),
                        ("처리단계", 0.076), ("실행횟수", 0.076), ("약관작업", 0.076), ("PDF변환작업", 0.076),
                        ("PDF병합작업", 0.076), ("우편작업", 0.076), ("FTP작업", 0.076), ("모바일작업", 0.076),
                        ("앱실행수", 0.076)
                    };

                    foreach (var colInfo in columnWidths)
                    {
                        var col = gdview.Columns.ColumnByFieldName(colInfo.Name);
                        if (col != null)
                        {
                            col.Width = Convert.ToInt32(grdwidth * colInfo.Ratio);
                        }
                    }
                }
                finally
                {
                    // 무조건 실행되므로, 위에서 오타나 에러가 나도 그리드가 멈추지 않고 정상 작동!
                    gdview.EndUpdate();
                }
            }
            catch (Exception)
            {
                실행여부 = "실패";
            }
            return 실행여부;
        }

        private async Task<DataTable> 오류작업DT1()
        {
            return await Task.Run(() =>
            {
                var table = new DataTable("작업리스트");

                // 컬럼 정의를 배열로 묶기
                (string name, Type type)[] columns =
                {
                    ("확인", typeof(bool)),
                    ("groupKey", typeof(string)),
                    ("처리구분", typeof(string)),
                    ("고객사", typeof(string)),
                    ("상태", typeof(string)),
                    ("전체건수", typeof(string)),
                    ("처리단계", typeof(string))
                };

                foreach (var (name, type) in columns)
                    table.Columns.Add(name, type);

                return table;
            });

        }
     
        private async Task<string> grd오류작업(int 인수)
        {
            string 실행여부 = "성공";
            try
            {
                DataTable table = null;
                if (인수 == 0)
                {
                    table = await Task.Run(() => 오류작업DT1());
                }
                grd오류작업리스트.DataSource = table;
                GridView gdview = new GridView();
                gdview = view오류작업리스트;
                gdview.BeginUpdate();
                gdview.GroupPanelText = "오류작업리스트(R클릭:재시작, 삭제, 새로고침)";
                gdview.Appearance.GroupPanel.Font = new Font("카이겐고딕 KR Regular", 10);
                gdview.Appearance.GroupPanel.ForeColor = System.Drawing.Color.DarkBlue;
                gdview.OptionsView.ShowIndicator = false; // 인디게이터 숨김
                gdview.Appearance.HeaderPanel.Font = new Font("카이겐고딕 KR Regular", 10);
                gdview.Appearance.HeaderPanel.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
                gdview.Appearance.HeaderPanel.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
                gdview.OptionsSelection.MultiSelectMode = GridMultiSelectMode.CellSelect;  // 멀티라인 선택
                gdview.OptionsSelection.MultiSelect = true;
                gdview.Appearance.SelectedRow.BackColor = System.Drawing.Color.White;
                gdview.Appearance.SelectedRow.ForeColor = System.Drawing.Color.Blue;
                gdview.ColumnPanelRowHeight = 25;       // 제목필드 크기 
                gdview.OptionsBehavior.EditorShowMode = DevExpress.Utils.EditorShowMode.Click; //원클릭

                int grdwidth = grd오류작업리스트.Width;
                // 컬럼 존재 체크 후 사이즈 적용 (안전)
                void SetColWidth(string fieldName, double ratio)
                {
                    var col = gdview.Columns.ColumnByFieldName(fieldName);
                    if (col != null) { col.Width = Convert.ToInt32(grdwidth * ratio); }
                }
                SetColWidth("확인", 0.08);
                SetColWidth("groupKey", 0.40);
                SetColWidth("처리구분", 0.10);
                SetColWidth("고객사", 0.10);
                SetColWidth("상태", 0.10);
                SetColWidth("전체건수", 0.10);
                SetColWidth("처리단계", 0.10);
                gdview.EndUpdate();
            }
            catch (Exception)
            {
                실행여부 = "실패";
                throw; ;
            }
            return 실행여부;

        }

        // =============================================================================
        //  에러메세지 출력:라인(공통)
        // =============================================================================
        private void 에러코드출력(System.Diagnostics.StackTrace 에러, out int 라인)
        {
            var frame = 에러.GetFrames()?.FirstOrDefault(f => f.GetFileLineNumber() > 0);   // 마지막 프레임 (실제 오류 발생 지점)
            라인 = frame?.GetFileLineNumber() ?? 0;            // 오류 발생 라인 번호
        }

        // =============================================================================
        //  진행불가오류메세지(공통)
        // =============================================================================
        private void 진행오류메세지출력()
        {
            메소드명 = "진행오류메세지출력";
            List<string> ERRTEXT = new List<string>();
            ERRTEXT.Add("");
            ERRTEXT.Add("EEEEEEEEEEEEEEEEEEEEEERRRRRRRRRRRRRRRRR   RRRRRRRRRRRRRRRRR        OOOOOOOOO     RRRRRRRRRRRRRRRRR   ");
            ERRTEXT.Add("E::::::::::::::::::::ER::::::::::::::::R  R::::::::::::::::R     OO:::::::::OO   R::♡천재소년♡::R  ");
            ERRTEXT.Add("E::::::::::::::::::::ER::::::RRRRRR:::::R R::::::RRRRRR:::::R  OO:::::::::::::OO R::::::RRRRRR:::::R ");
            ERRTEXT.Add("EE::::::EEEEEEEEE::::ERR:::::R     R:::::RRR:::::R     R:::::RO:::::::OOO:::::::ORR:::::R     R:::::R");
            ERRTEXT.Add("  E:::::E       EEEEEE  R::::R     R:::::R  R::::R     R:::::RO::::::O   O::::::O  R::::R     R:::::R");
            ERRTEXT.Add("  E:::::E               R::::R     R:::::R  R::::R     R:::::RO:::::O     O:::::O  R::::R     R:::::R");
            ERRTEXT.Add("  E::::::EEEEEEEEEE     R::::RRRRRR:::::R   R::::RRRRRR:::::R O:::::O     O:::::O  R::::RRRRRR:::::R ");
            ERRTEXT.Add("  E:::::::::::::::E     R:♥천재소년♥RR    R:::::::::::::RR  O:::::O     O:::::O  R:::::::::::::RR  ");
            ERRTEXT.Add("  E:::::::::::::::E     R::::RRRRRR::::/:R   R::::RRRRRR:::::RO:::::O     O:::::O  R::::RRRRRR:::::R");
            ERRTEXT.Add("  E::::::EEEEEEEEEE     R::::R     R:::::R  R::::R     R:♥::RO:::::O     O:::::O  R::::R     R:::::R");
            ERRTEXT.Add("  E:::::E               R::::R     R:::::R  R::::R     R:천::RO:::::O     O:::::O  R::::R     R:::::R");
            ERRTEXT.Add("  E:::::E       EEEEEE  R::::R     R:::::R  R::::R     R:재::RO::::::O   O::::::O  R::::R     R:::::R");
            ERRTEXT.Add("EE::::::EEEEEEEE:::::ERR:::::R     R:::::RRR:::::R     R:소::RO:::::::OOO:::::::ORR:::::R     R:::::R");
            ERRTEXT.Add("E::::::::::::::::::::ER::::::R     R:::::RR::::::R     R:년::R OO:♥천재소년♥:OOR::::::R     R:::::R");
            ERRTEXT.Add("E::::::♡천재소년♡::ER::::::R     R:::::RR::::::R     R:♡::R   OO:::::::::OO   R::::::R     R:::::R");
            ERRTEXT.Add("EEEEEEEEEEEEEEEEEEEEEERRRRRRRR     RRRRRRRRRRRRRRR     RRRRRRR     OOOOOOOOO     RRRRRRRR     RRRRRRR");
            ERRTEXT.Add("");

            for (int i = ERRTEXT.Count - 1; i >= 0; i--)
            {
                로그내용 = ERRTEXT[i];
                로그기록(로그내용, 0);
            }
        }

        // =============================================================================
        //  진행불가DB업데이트(공통)
        // =============================================================================
        private async Task<string> 진행불가DB업데이트(현재상태 상태)
        {
            string 실행여부 = "성공";
            int 재시도 = 0;

            while (true)
            {
                try
                {
                    string SqlStr = $@"
                        UPDATE data처리
                        SET 상태 = '오류', 실행횟수 = 실행횟수 + 1
                        WHERE groupKey = '{상태.groupKey}' AND 
                                처리구분 = '{상태.처리구분}'
                        ";
                    if (await 실행(() => DBUpdate(SqlStr, 상태.dataTable접속), "진행불가DB업데이트(DBUpdate)") == "실패")
                    {
                        throw new Exception("ER00_진행불가DB업데이트(DBUpdate) 오류, 로그참조");
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
                    throw;
                }
            }
            진행오류메세지출력();
            return 실행여부;
        }

        private void simpleButton1_Click(object sender, EventArgs e)
        {
            // 1. 날짜 추출 최적화
            // 폴더명(yyyy.MM, dd)과 파일명(yyMMdd)을 각각 만들기 위해 기준이 되는 날짜를 먼저 구합니다.
            DateTime targetDate = (logdate.EditValue is DateTime selectedDate)
                                  ? selectedDate
                                  : DateTime.Now;

            string dateSuffix = targetDate.ToString("yyMMdd");         // 파일명용 (예: 260415)
            string folderYearMonth = targetDate.ToString("yyyy.MM");   // 폴더 1뎁스 (예: 2026.04)
            string folderDay = targetDate.ToString("dd");              // 폴더 2뎁스 (예: 15)

            // 2. 경로 결합 최적화 (Path.Combine을 사용해 안전하게 경로 생성)
            // 생성 결과: c:\Easy약관Plus\program\LogFile\2026.04\15\
            string logDirectory = System.IO.Path.Combine(저장.로그경로, folderYearMonth, folderDay);
            string logFileName = $"({dateSuffix})맞춤약관로그.log";
            string fullLogPath = System.IO.Path.Combine(logDirectory, logFileName);

            // 3. 파일 존재 여부 체크
            if (!System.IO.File.Exists(fullLogPath))
            {
                // 사용자가 확인하기 쉽도록 메시지에 전체 경로를 포함해주는 것이 디버깅에 유리합니다.
                MessageBox.Show($"{targetDate:yyyy-MM-dd} 날짜의 로그 파일이 존재하지 않습니다.\n경로: {fullLogPath}", "알림", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                string editorPath = 저장.에디터경로?.Trim();

                // 4. 에디터 실행 로직
                if (!string.IsNullOrEmpty(editorPath) && System.IO.File.Exists(editorPath))
                {
                    // 지정된 에디터로 실행
                    System.Diagnostics.Process.Start(editorPath, $"\"{fullLogPath}\"");
                }
                else
                {
                    // 에디터 경로가 없거나 파일이 없으면 시스템 기본 프로그램(메모장 등)으로 실행
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(fullLogPath) { UseShellExecute = true });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"로그 파일을 여는 중 오류가 발생했습니다.\n{ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void tmp삭제_Click(object sender, EventArgs e)
        {
            완전한Temp삭제();
        }

        // =========================================================================
        // [신규 추가] 정기 재시작 전 Temp 폴더 무차별 삭제 (찌꺼기 청소)
        // =========================================================================
        private void 완전한Temp삭제()
        {

            //파일삭제 : del /q /f /s "%TEMP%\*" 2>nul 
            //폴더삭제 : for /D %p in ("%TEMP%\*") do rd /s /q "%p" 2>nul
            try
            {
                로그기록($"{DateTime.Now:yy.MM.dd HH:mm:ss} [시스템-알림] 정기 재시작 전 Temp 폴더 무차별 청소를 시작합니다.", 1);

                System.Diagnostics.ProcessStartInfo pri = new System.Diagnostics.ProcessStartInfo();
                pri.FileName = "cmd.exe";
                // 찌꺼기 파일과 빈 폴더까지 모두 싹 지우는 명령어 (에러 메시지 숨김 처리 포함)
                pri.Arguments = "/c del /q /f /s \"%TEMP%\\*\" 2>nul & for /D %p in (\"%TEMP%\\*\") do rd /s /q \"%p\" 2>nul";
                pri.CreateNoWindow = true;          // 검은 CMD 창 숨김
                pri.UseShellExecute = false;

                using (System.Diagnostics.Process p = System.Diagnostics.Process.Start(pri))
                {
                    로그기록($"{DateTime.Now:yy.MM.dd HH:mm:ss} [시스템-알림] Temp 폴더 청소중.....", 1);
                    // 삭제가 완료될 때까지 최대 15초간 대기 (동기화)
                    p.WaitForExit(15000);
                }

                로그기록($"{DateTime.Now:yy.MM.dd HH:mm:ss} [시스템-알림] Temp 폴더 청소 완료.", 1);
            }
            catch (Exception ex)
            {
                로그기록($"{DateTime.Now:yy.MM.dd HH:mm:ss} [오류] Temp 폴더 청소 중 예외 발생: {ex.Message}", 1);
            }
        }
    }


}