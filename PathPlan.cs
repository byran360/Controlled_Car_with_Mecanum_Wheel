using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SmartCar.Nav
{
    class PathPlan
    {
        public PathPlan()
        {

        }

        // 记录距离，找通道
        public void Start(ConPort conPort, IDrPort drPort, UrgPort urgPort)
        {
            // 起始位置(0,0,PI/2)
            KeyPoint keyPoint = new KeyPoint();
            keyPoint.RecordTxt(drPort); keyPoint.RecordExcel(drPort);    // 第零个点（起点）

            // 找通道
            AlignAisle align = new AlignAisle();
            align.Start();
            keyPoint.RecordTxt(drPort); keyPoint.RecordExcel(drPort);             // 第一个点

            // 通道内走，走到底
            Forward forward = new Forward();
            forward.EnterAilse(new KeyPoint(), 0, conPort, urgPort, drPort);             // 第二至四个点
            
            // 转第一个弯
            Turn turn = new Turn();     
            turn.TurnFirstRight(conPort, urgPort, drPort);                               // 第五个点

            // 继续前进 
            forward.EnterAilse(new KeyPoint(), 0, conPort, urgPort, drPort);             //第六至八个点

            // 转第二个弯
            turn.TurnSecondRight(conPort, urgPort, drPort);                                      // 第九个点
            keyPoint.RecordTxt(drPort); keyPoint.RecordExcel(drPort);                            // 第十个点

            // 对齐出通道
            forward.LeaveAilse(conPort, urgPort, drPort);                                        // 第十一个点
            keyPoint.RecordTxt(drPort); keyPoint.RecordExcel(drPort);                            // 第十二个点

        }

      
    }
}
