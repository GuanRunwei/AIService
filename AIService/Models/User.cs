
namespace AIService.Models
{
    using AIService.Enums;
    using AIService.Helper;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;

    public class User
    {   
        public User()
        {
            
        }
        public long Id { get; set; }//�û�Id
        public string Username { get; set; }//�û���
        public string Phonenumber { get; set; }//�ֻ�����
        public string Password { get; set; }//����
        public DateTime CreateTime { get; set; }//�û�����ʱ��
        public string Remark { get; set; }//�û�����ǩ��
        public string ImageUrl { get; set; }//�û�ͷ������
        public Gender Gender { get; set; }//�û��Ա�0��Ů��1���У�
        public int FansNumber { get; set; }//�û���˿����
        public int FollowNumber { get; set; }//�û���ע���û���
        public UserType UserType { get; set; } //�û����ͣ�0�Ǹ����û���1�ǹ�˾�û���
        //˵˵����TalkNumber��Redis�洢
        public string StockAge { get { return StockAgeCalculation.Age(Id); } }  //����
        [DefaultValue(0)]
        public int CoinNumber { get; set; }  //�������

    }
}
