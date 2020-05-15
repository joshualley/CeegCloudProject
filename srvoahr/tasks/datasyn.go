package tasks

import (
	"encoding/json"
	"fmt"
	"io/ioutil"
	"srvoahr/utils"
	"strconv"
)

const KEY string = "m5oAAAAAKE+A733t"

func createDbConn() []utils.DBHelper {
	dbHelpers := new(utils.DBHelpers)
	confPath := utils.GetRoot() + "enconf.json"
	//utils.WriteLogWithFn("SuccessfulLog", "加载配置文件 => "+confPath)
	jsonFile, _ := ioutil.ReadFile(confPath)
	json.Unmarshal(jsonFile, &dbHelpers)
	conns := dbHelpers.DbConnections
	for i := 0; i < len(conns); i++ {
		conns[i].HRConnStr = utils.AesDecrypt(conns[i].HRConnStr, KEY)
		conns[i].OAConnStr = utils.AesDecrypt(conns[i].OAConnStr, KEY)
	}
	return conns
}

//SynHrEmp2OA hr到oa同步员工信息
func SynHrEmp2OA() {
	conns := createDbConn()
	for _, dbHelper := range conns {
		sqlOa := "exec proc_czly_GetEmpInfo"
		data, err := dbHelper.QueryHr(sqlOa)
		if err != nil {
			break
		}
		var sqlHr string = ""
		for _, row := range data {
			sqlHr += fmt.Sprintf("exec proc_czly_SynEmpInfo "+
				"@FHrPID='%s',@FHrDeptID='%s',@FHrPostID='%s',"+
				"@FWorkDate='%s',@FJoinDate='%s',"+
				"@FGender='%s',@FBirthday='%s';",
				row["FHrPID"].(string), row["FHRDeptID"].(string), row["FHRPostID"].(string),
				row["FWorkDate"].(string), row["FJoinDate"].(string),
				row["FGender"].(string), row["FBirthday"].(string),
			)
		}
		if sqlHr != "" {
			err = dbHelper.ExecOa(sqlHr)
			if err != nil {
				utils.WriteLogWithFn("SuccessfulLog", sqlHr)
			}
		}
	}
}

//SynAll2HR 同步所有
func SynAll2HR() {
	conns := createDbConn()
	for _, dbHelper := range conns {
		hireSyn2HR(dbHelper)
		regularSyn2HR(dbHelper)
		fluctuationSyn2HR(dbHelper)
		resignSyn2HR(dbHelper)
	}
}

// 入职同步
func hireSyn2HR(dbHelper utils.DBHelper) {
	sqlOa := "exec proc_czly_GetSynData @FType='录用'"
	data, err := dbHelper.QueryOa(sqlOa)
	if err != nil {
		return
	}
	var sqlHr string = ""
	for _, row := range data {
		gender, _ := strconv.Atoi(row["FGender"].(string))
		gender++

		sqlHr = fmt.Sprintf("exec proc_czly_InsertEmpEnroll "+
			"@FOrgID='%s',@FEmpName='%s',@FIDCardNo='%s',@FPassportNo='%s',"+
			"@FGender='%d',@FBirthday='%s',@FPositionID='%s',@FAdminOrgUnitID='%s',@FTelNum='%s',"+
			"@FJobStartDate='%s',@FEnrollDate='%s',@FProbation='%s',@FPlanFormalDate='%s',"+
			"@FEmptypeID='%s',@FDescription='%s',@FSrcID='%s';",
			row["FHrOrgID"].(string), row["FName"].(string), row["FIdentityNum"].(string), row["FPassportNum"].(string),
			gender, row["FBirthday"].(string), row["FHrPostID"].(string), row["FHrDeptID"].(string), row["FPhone"].(string),
			row["FSocialDate"].(string), row["FEntryDate"].(string), row["FProbation"].(string), row["FRegularDate"].(string),
			row["FEmpType"].(string), row["FRemarks"].(string), row["FID"].(string),
		)

		err = dbHelper.ExecHr(sqlHr)
		if err == nil {
			sqlOa = fmt.Sprintf("update ora_t_OfferProcess set FIsSynHR=1 where FID='%s'", row["FID"].(string))
			dbHelper.ExecOa(sqlOa)
			utils.WriteLogWithFn("SuccessfulLog", sqlHr)
			utils.WriteLogWithFn("SuccessfulLog", sqlOa)
		}
	}

}

// 转正
func regularSyn2HR(dbHelper utils.DBHelper) {
	sqlOa := "exec proc_czly_GetSynData @FType='转正'"
	data, err := dbHelper.QueryOa(sqlOa)
	if err != nil {
		return
	}
	var sqlHr string = ""
	for _, row := range data {
		sqlHr = fmt.Sprintf("exec proc_czly_InsertEmpHire "+
			"@FPersonID='%s',@FOrgID='%s',@FBizDate='%s',"+
			"@FPositionID='%s',@FAdminOrgID='%s',@FDescription='%s',@FSrcID='%s';",
			row["FHrEmpID"].(string), row["FHrOrgID"].(string), row["FRegularDate"].(string),
			row["FHrAfterPostID"].(string), row["FHrAfterDeptID"].(string),
			row["FRemarks"].(string), row["FID"].(string),
		)
		err = dbHelper.ExecHr(sqlHr)
		if err == nil {
			sqlOa = fmt.Sprintf("update ora_t_Work set FIsSynHR=1 where FID='%s'", row["FID"].(string))
			dbHelper.ExecOa(sqlOa)
			utils.WriteLogWithFn("SuccessfulLog", sqlHr)
			utils.WriteLogWithFn("SuccessfulLog", sqlOa)
		}
	}

}

// 调职
func fluctuationSyn2HR(dbHelper utils.DBHelper) {
	sqlOa := "exec proc_czly_GetSynData @FType='调职'"
	data, err := dbHelper.QueryOa(sqlOa)
	if err != nil {
		return
	}
	var sqlHr string = ""
	for _, row := range data {
		sqlHr += fmt.Sprintf("exec proc_czly_InsertFluctuation "+
			"@FPersonID='%s',@FBizDate='%s',"+
			"@FReplaceAdminOrgID='%s',@FPositionID='%s',@FDescription='%s',@FSrcID='%s';",
			row["FHrApplyID"].(string), row["FInDate"].(string), row["FHrInDeptID"].(string),
			row["FHrInPostID"].(string), row["FRemarks"].(string), row["FID"].(string),
		)
		err = dbHelper.ExecHr(sqlHr)
		if err == nil {
			sqlOa = fmt.Sprintf("update ora_t_Transfer set FIsSynHR=1 where FID='%s'", row["FID"].(string))
			dbHelper.ExecOa(sqlOa)
			utils.WriteLogWithFn("SuccessfulLog", sqlHr)
			utils.WriteLogWithFn("SuccessfulLog", sqlOa)
		}
	}

}

// 离职
func resignSyn2HR(dbHelper utils.DBHelper) {
	sqlOa := "exec proc_czly_GetSynData @FType='离职'"
	data, err := dbHelper.QueryOa(sqlOa)
	if err != nil {
		return
	}
	var sqlHr string = ""
	for _, row := range data {
		sqlHr += fmt.Sprintf("exec proc_czly_InsertResignBiz "+
			"@FPersonID='%s',@FBizDate='%s',"+
			"@FOrgID='%s',@FDescription='%s',@FSrcID='%s';",
			row["FHrApplyID"].(string), row["FQuitDate"].(string),
			row["FHrOrgID"].(string), row["FRemarks"].(string), row["FID"].(string),
		)
		err = dbHelper.ExecHr(sqlHr)
		if err == nil {
			sqlOa = fmt.Sprintf("update ora_t_Dimission set FIsSynHR=1 where FID='%s'", row["FID"].(string))
			dbHelper.ExecOa(sqlOa)
			utils.WriteLogWithFn("SuccessfulLog", sqlHr)
			utils.WriteLogWithFn("SuccessfulLog", sqlOa)
		}
	}

}
