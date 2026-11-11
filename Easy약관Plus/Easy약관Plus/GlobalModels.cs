using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Easy약관Plus
{
    public class 약관정보
    {
        public int 개정일 { get; set; }
        public string 파일경로 { get; set; }
    }
    public class 레코드
    {
        public string 고객번호 { get; set; } = string.Empty;
        public string 상품코드 { get; set; } = string.Empty;
        public string 개정일 { get; set; } = string.Empty;
        public string 상품명 { get; set; } = string.Empty;
        public string 고객명 { get; set; } = string.Empty;
        public string 전화번호 { get; set; } = string.Empty;
        public string 이메일 { get; set; } = string.Empty;
        public string 우편번호 { get; set; } = string.Empty;
        public string 주소 { get; set; } = string.Empty;
        public string 종분류코드 { get; set; } = string.Empty;
        public string 서식구분 { get; set; } = string.Empty;
        public string 발송구분 { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string tmp02 { get; set; } = string.Empty;
        public string tmp03 { get; set; } = string.Empty;
        public string tmp04 { get; set; } = string.Empty;
        public string tmp05 { get; set; } = string.Empty;
        public string 담보갯수 { get; set; } = string.Empty;
        public string 담보코드 { get; set; } = string.Empty;
        public string 파일명 { get; set; } = string.Empty;
        public string 출력폴더 { get; set; } = string.Empty;
        public string 테이블인수 { get; set; } = string.Empty;
        public string 처리구분 { get; set; } = string.Empty;
        public string 고객사 { get; set; } = string.Empty;
        public string groupKey { get; set; } = string.Empty;
        public string 상태 { get; set; } = string.Empty;
        public string 약관작업 { get; set; } = string.Empty;
        public string PDF변환작업 { get; set; } = string.Empty;
        public string PDF병합작업 { get; set; } = string.Empty;
        public string 개별PDF작업 { get; set; } = string.Empty;
        public string 우편작업 { get; set; } = string.Empty;
        public string FTP작업 { get; set; } = string.Empty;
        public string 모바일작업 { get; set; } = string.Empty;
        public string 우편출력파일 { get; set; } = string.Empty;
        public string 모바일파일 { get; set; } = string.Empty;
        public string 전체약관경로 { get; set; } = string.Empty;
        public int 우편PDF장수 { get; set; }
        public int 그리드RowHandle { get; set; } = -1; // UI에서 맵핑할 그리드 행 인덱스
        public bool 중복스킵여부 { get; set; } = false; // 공통 모듈에서 체크해 줄 상태값

    }
    public class FTP상태
    {
        public string 현재FTP경로 { get; set; } = string.Empty;
        public string IP { get; set; } = string.Empty;
        public string ID { get; set; } = string.Empty;
        public string PW { get; set; } = string.Empty;
        public int PORT { get; set; }
    }
    public class 현재상태
    {
        public string groupKey { get; set; } = string.Empty;
        public string 고객사 { get; set; } = string.Empty;
        public string 상태 { get; set; } = string.Empty;
        public string 시작시간 { get; set; } = string.Empty;
        public string 완료시간 { get; set; } = string.Empty;
        public string 전체건수 { get; set; } = string.Empty;
        public int 병렬컴 { get; set; }
        public string 처리단위 { get; set; } = string.Empty;
        public string 처리단계 { get; set; } = string.Empty;
        public int 실행횟수 { get; set; }
        public string 약관작업 { get; set; } = string.Empty;
        public string PDF변환작업 { get; set; } = string.Empty;
        public string PDF병합작업 { get; set; } = string.Empty;
        public string 우편작업 { get; set; } = string.Empty;
        public string FTP작업 { get; set; } = string.Empty;
        public string 모바일작업 { get; set; } = string.Empty;
        public string 전체약관경로 { get; set; } = string.Empty;
        public int 앱실행수 { get; set; }
        public string 입력폴더 { get; set; } = string.Empty;
        public string 출력폴더 { get; set; } = string.Empty;
        public string 상태컴퓨터 { get; set; } = string.Empty;
        public string 처리Step { get; set; } = string.Empty;
        public string 처리여부 { get; set; } = string.Empty;
        public string dataTable접속 { get; set; } = string.Empty;
        public string 처리구분 { get; set; } = string.Empty;
        public string 고객Key { get; set; } = string.Empty;
        public string URLTable접속 { get; set; } = string.Empty;
        public 현재상태() { }

        public 현재상태(환경설정 설정)
        {
            this.상태컴퓨터 = 설정.상태컴퓨터;
            this.처리구분 = 설정.처리구분;
            this.dataTable접속 = 설정.dataTable접속;
            this.URLTable접속 = 설정.URLTable접속;
        }
    }
    public class 환경설정
    {

        public string 상태컴퓨터 { get; set; } = string.Empty;
        public string 처리구분 { get; set; } = string.Empty;
        public string dataTable접속 { get; set; } = string.Empty;
        public string URLTable접속 { get; set; } = string.Empty;

    }
    public class 저장
    {
        public static string 약관DB;
        public static string DB설정파일path;
        public static string 기본파일폴더;
        public static string 기본입력폴더;
        public static string 기본출력폴더 = @"d:\Easy약관Plus\Output\";
        public static string 사용컴퓨터;
        public static string 로그경로 = @"c:\Easy약관Plus\program\LogFile\";
        public static string 로그파일;
        public static string 현재진행;
        public static Dictionary<string, string> 상품별세션 = new Dictionary<string, string>();
        public static Dictionary<string, string> 설정값 = new Dictionary<string, string>();
        public static int 정기재시작 = 0;
        public static string 에디터경로 = "";
        
    }
    public class 우편파일
    {
        public string 고객번호 { get; set; } = string.Empty;
        public string 우편출력파일 { get; set; } = string.Empty;
        public int 우편페이지 { get; set; }
        public string 상품코드 { get; set; } = string.Empty;
    }
    public class 서식집계
    {
        public int 우편cnt { get; set; }
        public int 모바일cnt { get; set; }
        public int 이메일cnt { get; set; }
        public int 로컬cnt { get; set; }

    }

    
}
