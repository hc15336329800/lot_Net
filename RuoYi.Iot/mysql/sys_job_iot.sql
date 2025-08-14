DROP TABLE IF EXISTS sys_job_iot;
CREATE TABLE sys_job_iot (
  job_id        bigint(20)    NOT NULL COMMENT '任务ID',
  target_type   varchar(64)   DEFAULT '' COMMENT '目标类型',
  task_type     varchar(64)   DEFAULT '' COMMENT '任务类型',
  device_id     bigint(20)    DEFAULT NULL COMMENT '设备ID',
  product_id     bigint(20)    DEFAULT NULL COMMENT '所属产品ID',

  select_points varchar(500)  DEFAULT '' COMMENT '选择点位（ （ iot_product_point表的 point_key 字段））',
  trigger_source varchar(64)  DEFAULT '' COMMENT '触发源',
  status        char(1)       DEFAULT '0' COMMENT '状态',
  create_by     varchar(64)   DEFAULT '' COMMENT '创建者',
  create_time   datetime      DEFAULT NULL COMMENT '创建时间',
  update_by     varchar(64)   DEFAULT '' COMMENT '更新者',
  update_time   datetime      DEFAULT NULL COMMENT '更新时间',
  remark        varchar(500)  DEFAULT '' COMMENT '备注',
  PRIMARY KEY (job_id),
  CONSTRAINT fk_sys_job_iot_job FOREIGN KEY (job_id) REFERENCES sys_job(job_id)
) ENGINE=InnoDB COMMENT='定时任务IOT扩展表';