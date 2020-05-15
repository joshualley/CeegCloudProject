package main

import (
	"bufio"
	"encoding/json"
	"fmt"
	"io/ioutil"
	"os"
	"srvoahr/utils"
)

type DBHelpers struct {
	DbConnections []DBHelper
}

//DBHelper 数据库工具
type DBHelper struct {
	HRConnStr string
	OAConnStr string
}

const KEY string = "m5oAAAAAKE+A733t"

func main() {
	dbHelpers := new(DBHelpers)
	jsonFile, err := ioutil.ReadFile("conf.json")
	if err != nil {
		fmt.Println("不存在要加密的配置文件：conf.json！")
		return
	}
	// 创建JSON文件
	encryptFile, err := os.Create("enconf.json")
	defer encryptFile.Close()
	if err != nil {
		fmt.Printf("文件创建失败：%s\n", err.Error())
		return
	}
	err = json.Unmarshal(jsonFile, &dbHelpers)
	if err != nil {
		fmt.Printf("配置文件解析失败：%s\n", err.Error())
		return
	}
	for i := 0; i < len(dbHelpers.DbConnections); i++ {
		dbHelpers.DbConnections[i].HRConnStr = utils.AesEncrypt(dbHelpers.DbConnections[i].HRConnStr, KEY)
		dbHelpers.DbConnections[i].OAConnStr = utils.AesEncrypt(dbHelpers.DbConnections[i].OAConnStr, KEY)
	}

	encoder := json.NewEncoder(encryptFile)
	err = encoder.Encode(dbHelpers)
	if err != nil {
		fmt.Println("写入文件失败", err.Error())

	} else {
		fmt.Println("enconf.json文件写入成功！")
	}

	reader := bufio.NewReader(os.Stdin)
	reader.ReadString('\n')

}
