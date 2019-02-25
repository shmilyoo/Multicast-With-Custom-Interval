# Multicast-With-Custom-Interval

使用 WPF 开发的支持自定义非均匀发送间隔的组播测试软件  
Multicast test software developed using WPF that supports custom non-uniform send intervals

## install

.NET Framework 4.0

## 功能

- 支持 ICMP V3
- 支持大于 MTU 的超大数据包
- 支持指定源和任意源组播
- 发送间隔精确度<1ms，可定义<1ms 的时间间隔
- 发送间隔支持非均匀定义，如定义为 1;2;3，发送的包间隔为 "包 1ms 包 2ms 包 3ms 包 1ms 包 2ms 包 3ms 包 1ms 包 2ms 包 3ms 包。。。。"
- 支持接收数据统计，支持乱序、错误包等提示统计

## 截图

# 界面

![](http://ww1.sinaimg.cn/large/566418e8gy1g0imxd9dntj20gn0dsgmp.jpg)

# 非均匀发送间隔定义

假设定义发送间隔为 **1;2;3;4;5** ，在抓包软件中查看包间隔如下  
![](http://ww1.sinaimg.cn/large/566418e8gy1g0in4n8xwfj206o08mweg.jpg)
