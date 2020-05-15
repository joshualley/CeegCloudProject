package main

import (
	"os"
	"srvoahr/tasks"
	"srvoahr/utils"

	"github.com/jander/golog/logger"
	"github.com/jasonlvhit/gocron"
	"github.com/kardianos/service"
)

type program struct{}

func (p *program) Start(s service.Service) error {
	go p.run()
	return nil
}

func synTask() {
	utils.WriteLogWithFn("SuccessfulLog", "执行员工信息同步OA => HR")
	tasks.SynAll2HR()
}

func synTask2() {
	utils.WriteLogWithFn("SuccessfulLog", "执行同步HR => OA")
	tasks.SynHrEmp2OA()
}

func (p *program) run() {
	// TODO
	AllService()
}

func (p *program) Stop(s service.Service) error {
	return nil
}

func RunService() {
	svcConfig := &service.Config{
		Name:        "OA-HR-SYC",    //服务显示名称
		DisplayName: "变压器OA-HR同步服务", //服务名称
		Description: "变压器OA-HR同步服务", //服务描述
	}

	prg := &program{}
	s, err := service.New(prg, svcConfig)
	if err != nil {
		logger.Fatal(err)
	}

	if err != nil {
		logger.Fatal(err)
	}

	if len(os.Args) > 1 {
		switch os.Args[1] {
		case "install":
			s.Install()
			logger.Info("服务安装成功!")
			s.Start()
			logger.Info("服务启动成功!")
			break
		case "start":
			s.Start()
			logger.Info("服务启动成功!")
			break
		case "stop":
			s.Stop()
			logger.Info("服务关闭成功!")
			break
		case "restart":
			s.Stop()
			logger.Info("服务关闭成功!")
			s.Start()
			logger.Info("服务启动成功!")
			break
		case "remove":
			s.Stop()
			logger.Info("服务关闭成功!")
			s.Uninstall()
			logger.Info("服务卸载成功!")
			break
		}
		return
	}

	err = s.Run()
	if err != nil {
		logger.Error(err)
	}
}

func AllService() {
	logger.Info("Starting...")
	scheduler := gocron.NewScheduler()
	scheduler.Every(1).Minutes().Do(synTask)
	scheduler.Every(1).Days().At("01:00:01").Do(synTask2)
	scheduler.Start()
}

func main() {
	// AllService()
	RunService()
}
