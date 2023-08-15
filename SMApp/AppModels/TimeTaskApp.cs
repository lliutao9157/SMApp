using System.Reflection;

using System;

namespace SMApp
{
    public class TimeTaskApp
    {
        public string Name { get; set; }
        public string Content { get; set; }
        public object instance { get; set; }
        public MethodInfo methodInfo { get; set; }
        public bool CanExcute { get; set; }
        public DateTime? ExcuteTime { get; set; }
        public AppInfo AppInfo { get; set; }
        //执行等待时间
        public int? Time { get; set; }
        //执行次数
        public int? Times { get; set; }
        /// <summary>
        /// 定时任务执行之前
        /// </summary>
        public event Action<TimeTaskApp> OnBeforExucte;
        /// <summary>
        /// 定时任务执行之后
        /// </summary>
        public event Action<TimeTaskApp,Exception> OnAfterExcute;
        public bool? Iscycle { get; set; }
        public bool? Isopen { get; set; }
       
        #region public method
        public void Taskset(bool isopen,bool? iscycle=null,int? time=null,DateTime? excutetime=null,int? times=null)
        {
            this.Isopen = isopen;
            if(time!=null) this.Time = time;
            this.ExcuteTime = excutetime;
            if (iscycle != null) this.Iscycle = iscycle;
            if (times != null) this.Times = times;
        }
        #endregion

        #region internal
        internal void BeforExcute(TimeTaskApp app)
        {
            if (OnBeforExucte != null) OnBeforExucte(app);
        }
        internal void AfterExcute(TimeTaskApp app,Exception ex)
        {
            if (OnAfterExcute != null) OnAfterExcute(app,ex);
        }

        #endregion

    }

}
