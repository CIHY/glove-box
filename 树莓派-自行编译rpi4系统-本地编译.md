## 目录
- [开始之前](#开始之前)
- [磁盘分区](#磁盘分区)
- [构建内核](#构建内核)
- [制作rootfs](#制作rootfs)
- [安装内核以及驱动](#安装内核以及驱动)
- [参考资料](#参考资料)
---

## 开始之前
编译设备需要具有网络连接以及3GB的空闲磁盘空间
因为整个过程将不生成镜像文件，直接把系统写入磁盘，所以需要一张额外的空白MicroSD储存卡（推荐至少8GB），以及相应的读卡器
为了节省时间，除`/boot`外，`/home`、`/proc`等都将直接包含在`/`下，不额外进行分区
> *“在一个有系统的树莓派上构建另一个全新的树莓派系统，就像是母鸡下蛋孵小鸡一样！”*

## 磁盘分区
将准备好的sd卡放入读卡器，插入编译设备的USB口让系统进行识别，准备开始分区工作
如果没有接入其他的储存设备，那么新的设备将在`/dev/sda`处
执行以下命令开始磁盘分区：`sudo /sbin/fdisk /dev/sda`
成功进入`fdisk`后，首先输入`p`并回车，让`fdisk`打印磁盘信息，记录`磁盘标识符 (Disk identifier)`的值（如：`0x180be195`），这在之后会用到
![fdisk打印信息](https://img2023.cnblogs.com/blog/1364069/202309/1364069-20230922000149253-405971435.png)

观察`磁盘标签类型 (Disklabel type)`的值，如果不是`dos`则输入`o`并回车，将磁盘的分区表修改为DOS，之后便可进行分区

- 输入`n`并回车，创建第一个分区（启动分区）
    `分区类型 (Partition type)`与`分区号 (Partition number)`以及`First sector (第一个扇区)`选项保持默认，无需输入任何数值，直接回车
    在`最后一个扇区 (Last sector)`选项输入`526335`，回车完成分区创建，此时将回到`fdisk`主菜单
    回到主菜单后，输入`t`并回车
    `fdisk`将自动跳转到`Hex代码或别名 (Hex code or alias)`选项，输入`b`，回车修改分区文件系统类型为`W95 FAT32`
    完成分区文件系统类型更改后，将再次回到`fdisk`主菜单
    输入`a`并回车，`fidks`将自动为分区添加“可启动”标识
- 输入`n`并回车，创建第二个分区（系统分区）
    `分区类型 (Partition type)`与`分区号 (Partition number)`以及`First sector (第一个扇区)`依旧保持默认选项
    在`最后一个扇区 (Last sector)`选项时，如不使用交换分区，则直接回车，完成分区工作；如需使用交换分区，则输入结束扇区的扇区号并回车完成第二分区分区工作
- 【可跳过】输入`n`并回车，创建第三个分区（交换分区）
    所有选项全部保持默认，一路回车完成创建工作并回到`fdisk`主菜单
    回到主菜单后，输入`t`并回车
    在`分区号 (Partition number)`选项输入`3`，回车
    在`Hex代码或别名 (Hex code or alias)`选项输入`82`，回车完成分区工作

执行完上述操作后，输入`w`并回车，让`fdisk`将更改写入磁盘，写入完成后，`fdisk`将自动退出

最后使用`mkfs`系列命令进行分区格式化
- 格式化启动分区：`sudo /sbin/mkfs.vfat /dev/sda1`
- 格式化系统分区：`sudo /sbin/mkfs.ext4 /dev/sda2`

![新接入的储存设备（非空白）](https://img2023.cnblogs.com/blog/1364069/202309/1364069-20230921235822383-1183370937.png)

## 构建内核
首先执行`sudo apt install git bc bison flex libssl-dev make`获取构建内核所需要的依赖
当命令执行完成后，执行`git clone --depth=1 https://github.com/raspberrypi/linux`下载内核源码

> 如果需要获取不同版本的源码，请参考该命令：
> `git clone --depth=1 --branch <branch> https://github.com/raspberrypi/linux`
> 在`Github`的版本列表中一众以`rpi-`开头的分支才是内核源码
> ![Github版本列表](https://img2023.cnblogs.com/blog/1364069/202309/1364069-20230922001435604-24307661.png)

源码下载完成后，对于不同的设备，接下来的操作也会有些许不同

构建32位系统：
- 如果正在为`Raspberry Pi 1`、`Raspberry Pi Zero`、`Raspberry Pi Zero W`、`Raspberry Pi Compute Module 1`构建32位操作系统，请执行以下命令：
    ```shell
    cd linux
    KERNEL=kernel
    make bcmrpi_defconfig
    ```
- 如果正在为`Raspberry Pi 2`、`Raspberry Pi 3`、`Raspberry Pi 3+`、`Raspberry Pi Zero 2 W`、`Raspberry Pi Compute Modules 3`、`Raspberry Pi Compute Modules 3+`构建32位操作系统，请执行以下命令：
    ```shell
    cd linux
    KERNEL=kernel7
    make bcm2709_defconfig
    ```
- 如果正在为`Raspberry Pi 4`、`Raspberry Pi 400`、`Raspberry Pi Compute Module 4`构建32位操作系统，请执行以下命令
    ```shell
    cd linux
    KERNEL=kernel7l
    make bcm2711_defconfig
    ```

构建64位系统：
- 如果正在为`Raspberry Pi 3`、`Raspberry Pi 3+`、`Raspberry Pi 4`、`Raspberry Pi 400`、`Raspberry Pi Zero 2 W`、`Raspberry Pi Compute Modules 3`、`Raspberry Pi Compute Modules 3+`、`Raspberry Pi Compute Modules 4`构建64位操作系统，请执行以下命令：
    ```shell
    cd linux
    KERNEL=kernel8
    make bcm2711_defconfig
    ```

在正式构建前，可以使用`menuconfig`修改内核的配置

> 注意，该步骤并不是必要的，有时候保持默认配置并不是一个坏的选择

开始使用`menuconfig`前，请执行`sudo apt install libncurses5-dev`以确保必要依赖
命令执行完成后，即可使用`make menuconfig`修改内核配置
![menuconfig界面](https://img2023.cnblogs.com/blog/1364069/202309/1364069-20230922001743189-1406926705.png)

> *（以下内容为[树莓派官方文档](https://www.raspberrypi.com/documentation/computers/linux_kernel.html#using-menuconfig)中对`menuconfig`的介绍，原文英文，由机器翻译）*
> `menuconfig`实用程序具有简单的键盘导航功能。经过简短的编译后，您将看到一个子菜单列表，其中包含您可以配置的所有选项；有很多，所以花点时间仔细阅读并熟悉一下。
> 使用箭头键进行导航，使用Enter键进入子菜单（由`--->`指示），使用Escape两次进入上一级或退出，使用空格键循环选项的状态。有些选项有多个选项，在这种情况下，它们将显示为子菜单，并按Enter键选择一个选项。您可以在大多数条目上按`h`来获取有关特定选项或菜单的帮助。
> 在你的第一次尝试中，要抵制启用或禁用很多东西的诱惑；破坏配置相对容易，所以从小处着手，熟悉配置和构建过程。

完成内核配置后，即可开始进行编译工作

- 对于32位内核，请执行以下命令：
    ```shell
    make -j4 zImage modules dtbs
    ```
- 对于64位内核，请执行以下命令：
    ```shell
    make -j4 Image.gz modules dtbs
    ```

内核的编译将耗费大量的时间以及CPU资源，请保持编译设备拥有充足的电源以及**良好的散热**

## 制作rootfs
在内核编译完成后，使用`cd ../`回到上一级目录
在开始前请先执行`sudo apt install debootstrap`安装所需依赖
安装完成后，创建`sdcard`文件夹并`cd`进入，并在其中创建`rootfs`文件夹，用于挂载sd卡
执行`sudo mount /dev/sda2 rootfs/`挂载已格式化的文件系统
接着使用`debootstrap`构建基础系统

> *命令参考：*
> `debootstrap --arch [体系结构] [发行版本代号] [目录] [下载源]`

> `[下载源]`参数如不填则将使用默认的下载站点
> 下方链接中包含了所有可用的下载源，替换默认源时请使用离自己最近的站点：
> [https://www.debian.org/mirror/list](https://www.debian.org/mirror/list)

> 命令执行成功后将会输出以下信息：
> `I: Base system installed successfully.`

`debootstrap`在安装后存在于`/sbin`下，并且需要`root`权限
这里以构建`arm64`体系结构，版本为`bullseye`的基础系统作为示例参考：
```shell
sudo /sbin/debootstrap --arch=arm64 bullseye rootfs/
```
![正在运行中的debootstrap](https://img2023.cnblogs.com/blog/1364069/202309/1364069-20230922002920968-1573061449.png)

在基础系统构建完成后，下载[ch-mount.sh](https://github.com/psachin/bash_scripts/blob/master/ch-mount.sh)到文件夹中，用以进入制作好的rootfs
```shell
wget https://raw.githubusercontent.com/psachin/bash_scripts/master/ch-mount.sh
```

使用`chmod +x ./ch-mount.sh`赋予脚本执行权限，再使用`./ch-mount.sh -m rootfs/`生成临时挂载点并进入rootfs

> 脚本执行时会请求`root`权限，但不要以`root`身份执行脚本
> 进入rootfs后，可能会出现`I have no name!`字样，为正常现象
> *接下来如无特殊说明，本节内的所有操作均在rootfs内的系统中完成*

![成功执行的ch-mount.sh](https://img2023.cnblogs.com/blog/1364069/202309/1364069-20230922003201576-414605147.png)

进入rootfs后，需先执行`apt install ca-certificates`为rootfs内的系统安装CA证书
证书安装完毕后，便可以打开`/etc/apt/source.list`文件更换镜像源
更换镜像源后，执行`apt update`更新软件列表，完成后继续安装必要程序
```shell
apt install dhcpcd5 locales sudo ssh wget
```
执行`useradd`添加用户，例如：`useradd nolen`
修改新用户的密码，例如：`passwd nolen`
打开`/etc/sudoers`文件，在`# User privilege specification`下方新增一行，允许用户使用`sudo`命令：
```
nolen   ALL=(ALL:ALL) ALL
```
![允许新用户使用sudo](https://img2023.cnblogs.com/blog/1364069/202309/1364069-20230922003732089-1871913423.png)

> 在修改sudoer文件时，可能遭遇`W10: Warning: Changing a readonly file`警告
> 这时只需退出编辑器，使用`chmod 640 /etc/sudoers`修改sudoers文件的权限即可
> 不要忘记修改回去：`chmod 440 /etc/sudoers`


打开`/etc/fstab`文件，修改其中内容，让内核能够正常挂载文件系统，例如：
```
# UNCONFIGURED FSTAB FOR BASE SYSTEM
proc /proc proc defaults 0 0
PARTUUID=180be195-01 /boot vfat defaults 0 2
PARTUUID=180be195-02 / ext4 defaults,noatime 0 1
PARTUUID=180be195-03 swap swap defaults 0 0
```

> 在示例中
> `180be195`是之前分区时获取到的磁盘id：`0x180be195`
> 后方的`-01`、`-02`代表着磁盘的第一个分区（启动分区）、第二个分区（系统分区）
> `180be195-03`是交换分区

> 格式参考：
> `<file system>    <mount point>    <type>    <options>    <dump>    <pass>`
> `<file system>` - 为要挂载的分区或存储设备，即相应的UUID
> `<mount point>` - 为挂载点
> `<type>` - 为文件系统类型
> `<options>` - 为挂载时使用的参数
> `<dump>` - 为是否做备份
> `<pass>` - 为需要检查的文件系统的检查顺序

![fstab文件内容](https://img2023.cnblogs.com/blog/1364069/202309/1364069-20230922005419299-1380246290.png)

最后可以修改`/etc/hostname`修改主机名称，例如：`echo 'my-rpi' > /etc/hostname`
以及在`/etc/hosts`中新增`127.0.0.1    my-rpi`以防止`sudo`命令出现警告信息

完成后即可输入`exit`退出rootfs，然后使用`./ch-mount -u rootfs/`卸载掉临时挂载点

## 安装内核以及驱动
执行`sudo mount /dev/sda1 boot/`挂载已格式化的文件系统
使用`cd ../linux`切换到内核源码目录，为rootfs中的系统安装必要模块

- 对于32位系统，请执行以下命令：
    ```shell
    sudo make ARCH=arm INSTALL_MOD_PATH=../sdcard/rootfs modules_install
    ```
- 对于64位系统，请执行以下命令：
    ```shell
    sudo make ARCH=arm64 INSTALL_MOD_PATH=../sdcard/rootfs modules_install
    ```

在系统模块安装完成后，即可将内核以及驱动放入启动分区

- 对于32位系统，请执行以下命令：
    ```shell
    sudo cp arch/arm/boot/dts/*.dtb ../sdcard/boot/
    sudo cp arch/arm/boot/dts/overlays/*.dtb* ../sdcard/boot/overlays/
    sudo cp arch/arm/boot/dts/overlays/README ../sdcard/boot/overlays/
    sudo cp arch/arm/boot/zImage ../sdcard/boot/$KERNEL.img
    ```
- 对于64位系统，请执行以下命令：
    ```shell
    sudo cp arch/arm64/boot/dts/broadcom/*.dtb ../sdcard/boot/
    sudo cp arch/arm64/boot/dts/overlays/*.dtb* ../sdcard/boot/overlays/
    sudo cp arch/arm64/boot/dts/overlays/README ../sdcard/boot/overlays/
    sudo cp arch/arm64/boot/Image.gz ../sdcard/boot/$KERNEL.img
    ```

虽然内核以及驱动都以安装完成，但缺少启动必要文件
`cd ../`回到上一级，执行`git clone --depth=1 https://github.com/raspberrypi/firmware`获取文件
当文件拉取完成后，执行以下命令将文件放入启动分区：
```shell
sudo cp firmware/boot/*.dat sdcard/boot/
sudo cp firmware/boot/*.elf sdcard/boot/
```

文件复制完成后，使用`cd`命令切换到已挂载的启动分区内，创建启动配置文件`cmdline.txt`以及内核配置文件`config.txt`
```shell
cd sdcard/boot/
sudo touch cmdline.txt
sudo touch config.txt
```
![制作完成的启动分区](https://img2023.cnblogs.com/blog/1364069/202309/1364069-20230922010155351-1204184098.png)


打开并修改`cmdline.txt`文件内容：
```
dwc_otg.lpm_enable=0 console=serial0,115200 console=tty1 root=PARTUUID=180be195-02 rootfstype=ext4 elevator=deadline fsck.repair=yes rootwait quiet splash plymouth.ignore-serial-consoles psi=1
```

> `cmdline.txt`中`root=PARTUUID=`选项的值指向的是磁盘系统分区，注意替换

打开并修改`config.txt`文件内容：
- 对于32位系统：
    ```ini
    [all]
    arm_64bit=0
    framebuffer_width=1280
    framebuffer_height=720
    disable_overscan=1
    dtparam=audio=on
    ```
- 对于64位系统
    ```ini
    [all]
    arm_64bit=1
    framebuffer_width=1280
    framebuffer_height=720
    disable_overscan=1
    dtparam=audio=on
    ```

完成后回到上一级目录，卸载各文件系统，完成系统制作
```shell
cd ../
sudo umount boot/
sudo umount rootfs/
```

## 参考资料
[Raspberry Pi Documentation - The Linux kernel](https://www.raspberrypi.com/documentation/computers/linux_kernel.html)
[使用debootstrap手动安装debian系统 - 知乎](https://zhuanlan.zhihu.com/p/623363577)
[debootstrap制作编译环境 - 知乎](https://zhuanlan.zhihu.com/p/402584397)
[debootstrap 制作arm64位根文件系统_debian arm 编译_人生长恨水的博客-CSDN博客](https://blog.csdn.net/qq_36956154/article/details/100606619)
[树莓派最小启动依赖制作_start.elf-CSDN博客](https://blog.csdn.net/ddddfang/article/details/90750917)
[树莓派（Raspberry Pi 4 Model B）编译64位内核Kernel_树莓派 交叉编译 aarch64_人生长恨水的博客-CSDN博客](https://blog.csdn.net/qq_36956154/article/details/100105186)
[从内核开始构建树梅派3的64位系统 - 硬件相关 - ParrotSec中文社区](https://parrotsec-cn.org/t/3-64/2331)
