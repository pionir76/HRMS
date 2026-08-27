namespace HRMS.Modules.Auth.Models;

//--------------------------------------------------------------------------------//
// 사용자-담당장비 다대다 관계. 별도 Id 없이 (UserId, EquipmentId) 자체를 기본키로 쓴다.
// 한 사용자가 여러 장비를, 한 장비를 여러 사용자가 담당할 수 있다.
//--------------------------------------------------------------------------------//
public class UserEquipment
{
    public int UserId { get; set; }
    public int EquipmentId { get; set; }
}
