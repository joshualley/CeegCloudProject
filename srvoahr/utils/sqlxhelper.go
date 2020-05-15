package utils

import (
	"database/sql"
	"fmt"

	_ "github.com/denisenkom/go-mssqldb"
)

type DBHelpers struct {
	DbConnections []DBHelper
}

//DBHelper 数据库工具
type DBHelper struct {
	HRConnStr string
	OAConnStr string
}

//QueryOa 从OA数据库查询，返回数据
func (d *DBHelper) QueryOa(sqlStr string, args ...interface{}) ([]map[string]interface{}, error) {
	db, err := sql.Open("mssql", d.OAConnStr)
	defer db.Close()
	if err != nil {
		//panic(connErr)
		WriteLog(fmt.Sprintf("%s", err))
		return nil, err
	}
	if args != nil {
		rows, err := db.Query(sqlStr, args)
		if err != nil {
			//panic(queryErr)
			WriteLog(fmt.Sprintf("%s => [%s]", err, sqlStr))
			return nil, err
		}
		return d.getResult(rows), nil
	}

	rows, err := db.Query(sqlStr)
	if err != nil {
		//panic(queryErr)
		WriteLog(fmt.Sprintf("%s => [%s]", err, sqlStr))
		return nil, err
	}
	return d.getResult(rows), nil

}

//ExecOa 从OA数据库查询，不返回数据
func (d *DBHelper) ExecOa(sqlStr string, args ...interface{}) error {
	db, err := sql.Open("mssql", d.OAConnStr)
	defer db.Close()
	if err != nil {
		//panic(connErr)
		WriteLog(fmt.Sprintf("%s", err))
		return err
	}
	if args != nil {
		_, err = db.Exec(sqlStr, args)
	} else {
		_, err = db.Exec(sqlStr)
	}
	if err != nil {
		//panic(queryErr)
		WriteLog(fmt.Sprintf("%s => [%s]", err, sqlStr))
		return err
	}
	return nil
}

//QueryHr 从HR数据库查询，返回数据
func (d *DBHelper) QueryHr(sqlStr string, args ...interface{}) ([]map[string]interface{}, error) {
	db, err := sql.Open("mssql", d.HRConnStr)
	defer db.Close()
	if err != nil {
		//panic(connErr)
		WriteLog(fmt.Sprintf("%s", err))
		return nil, err
	}
	if args != nil {
		rows, err := db.Query(sqlStr, args)
		if err != nil {
			//panic(queryErr)
			WriteLog(fmt.Sprintf("%s => [%s]", err, sqlStr))
			return nil, err
		}
		return d.getResult(rows), nil
	}

	rows, err := db.Query(sqlStr)
	if err != nil {
		//panic(queryErr)
		WriteLog(fmt.Sprintf("%s => [%s]", err, sqlStr))
		return nil, err
	}
	return d.getResult(rows), nil

}

//ExecHr 从HR数据库查询，不返回数据
func (d *DBHelper) ExecHr(sqlStr string, args ...interface{}) error {
	db, err := sql.Open("mssql", d.HRConnStr)
	defer db.Close()
	if err != nil {
		//panic(connErr)
		WriteLog(fmt.Sprintf("%s", err))
		return err
	}
	if args != nil {
		_, err = db.Exec(sqlStr, args)
	} else {
		_, err = db.Exec(sqlStr)
	}
	if err != nil {
		//panic(queryErr)
		WriteLog(fmt.Sprintf("%s => [%s]", err, sqlStr))
		return err
	}
	return nil
}

func (d *DBHelper) getResult(query *sql.Rows) []map[string]interface{} {
	column, _ := query.Columns()              //读出查询出的列字段名
	values := make([][]byte, len(column))     //values是每个列的值，这里获取到byte里
	scans := make([]interface{}, len(column)) //因为每次查询出来的列是不定长的，用len(column)定住当次查询的长度
	for i := range values {                   //让每一行数据都填充到[][]byte里面
		scans[i] = &values[i]
	}
	results := make([]map[string]interface{}, 0) //最后得到的[]map
	//循环，让游标往下移动
	for query.Next() {
		//query.Scan查询出来的不定长值放到scans[i] = &values[i],也就是每行都放在values里
		if err := query.Scan(scans...); err != nil {
			//fmt.Println(err)
			WriteLog(fmt.Sprintf("%s", err))
			return results
		}
		row := make(map[string]interface{}) //每行数据
		for k, v := range values {          //每行数据是放在values里面，现在把它挪到row里
			key := column[k]
			row[key] = string(v)
		}
		results = append(results, row)
	}
	return results
}
