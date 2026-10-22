namespace HRMS.Modules.Equipment.Models;

// 모든 압축기 공통의 고정 채널 번호. CH01~07 각각의 의미(흡입온도/토출온도/오일온도/흡입압력/토출압력/오일압력/운전전류)는
// overview.md 8.1 "센서 채널 정의" 표 참고. 개수가 늘거나 줄 일이 없어 int가 아니라 enum으로 고정했다.
public enum ChannelNo
{
    CH01 = 1,
    CH02,
    CH03,
    CH04,
    CH05,
    CH06,
    CH07
}
