package utils

import (
	"io"
	"os"
	"os/exec"
	"strings"
	"time"
)

const (
	//LOGPATH  LOGPATH/time.Now().Format(FORMAT)/*.log
	LOGPATH = "log/"
	// LOGPATH = "D:/WorkSpace/GoProj/srvoahr/log/"
	//FORMAT
	FORMAT = "200601"
	//LineFeed 换行
	LineFeed = "\r\n"
)

// GetRoot获取程序根路径
func GetRoot() string {
	str, _ := exec.LookPath(os.Args[0])
	pathes := strings.Split(str, "\\")
	root := ""
	for i := 0; i < len(pathes)-1; i++ {
		root += pathes[i] + "/"
	}
	return root
}

//以天为基准,存日志
func getPath() string {
	return GetRoot() + LOGPATH + time.Now().Format(FORMAT) + "/"
}

// WriteLog 写入日志
func WriteLog(msg string) error {
	filename := "ErrorLog"
	return WriteLogWithFn(filename, msg)
}

// WriteLogWithFn : WriteLog return error
func WriteLogWithFn(fileName, msg string) error {
	fileName += "_" + time.Now().Format("20060102") + ".log"
	path := getPath()
	if !IsExist(path) {
		return CreateDir(path)
	}
	var (
		err error
		f   *os.File
	)

	f, err = os.OpenFile(path+fileName, os.O_CREATE|os.O_WRONLY|os.O_APPEND, 0666)
	msg = "[" + time.Now().Format("15:04:05") + "]\t" + msg
	_, err = io.WriteString(f, LineFeed+msg)

	defer f.Close()
	return err
}

//CreateDir  文件夹创建
func CreateDir(path string) error {
	err := os.MkdirAll(path, os.ModePerm)
	if err != nil {
		return err
	}
	os.Chmod(path, os.ModePerm)
	return nil
}

//IsExist  判断文件夹/文件是否存在  存在返回 true
func IsExist(f string) bool {
	_, err := os.Stat(f)
	return err == nil || os.IsExist(err)
}
