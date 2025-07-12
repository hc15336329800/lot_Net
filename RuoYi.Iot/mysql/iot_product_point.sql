/*
 Navicat Premium Data Transfer

 Source Server         : localhost
 Source Server Type    : MySQL
 Source Server Version : 80013
 Source Host           : 127.0.0.1:3306
 Source Schema         : ry_net_sass

 Target Server Type    : MySQL
 Target Server Version : 80013
 File Encoding         : 65001

 Date: 12/07/2025 18:30:29
*/

SET NAMES utf8mb4;
SET FOREIGN_KEY_CHECKS = 0;

-- ----------------------------
-- Table structure for iot_product_point
-- ----------------------------
DROP TABLE IF EXISTS `iot_product_point`;
CREATE TABLE `iot_product_point`  (
  `id` bigint(20) NOT NULL COMMENT '主键',
  `product_id` bigint(20) NOT NULL COMMENT '所属产品ID',
  `point_name` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL COMMENT '点位名称',
  `point_key` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL COMMENT '点位标识(唯一)',
  `variable_type` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL COMMENT '变量类型（如物联点位/属性/开关等）',
  `data_type` varchar(20) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL COMMENT '数据类型（数值/文本/开关/时间/日期/频谱等）',
  `unit` varchar(20) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NULL DEFAULT NULL COMMENT '单位',
  `default_value` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NULL DEFAULT NULL COMMENT '默认值',
  `decimal_digits` int(11) NULL DEFAULT NULL COMMENT '小数位数',
  `max_value` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NULL DEFAULT NULL COMMENT '最大值',
  `min_value` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NULL DEFAULT NULL COMMENT '最小值',
  `slave_address` int(11) NULL DEFAULT NULL COMMENT '从机地址',
  `function_code` int(11) NULL DEFAULT NULL COMMENT '功能码',
  `data_length` int(11) NULL DEFAULT NULL COMMENT '数据位数',
  `register_address` int(11) NULL DEFAULT NULL COMMENT '寄存器地址',
  `byte_order` varchar(10) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NULL DEFAULT NULL COMMENT '字节顺序（ABCD/BADC等）',
  `signed` tinyint(1) NULL DEFAULT 0 COMMENT '有无符号(0无1有)',
  `read_type` varchar(10) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NULL DEFAULT '读写' COMMENT '读写类型（只读/只写/读写）',
  `storage_mode` varchar(20) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NULL DEFAULT '全部存储' COMMENT '存储方式（全部/变化）',
  `display_on_dashboard` tinyint(1) NULL DEFAULT 0 COMMENT '看板展示(0否1是)',
  `collect_formula` varchar(200) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NULL DEFAULT NULL COMMENT '采集公式',
  `control_formula` varchar(200) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NULL DEFAULT NULL COMMENT '控制公式',
  `status` varchar(1) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL DEFAULT '0' COMMENT '状态',
  `del_flag` varchar(1) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL DEFAULT '0' COMMENT '删除标志',
  `create_by` varchar(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NULL DEFAULT NULL COMMENT '创建者',
  `create_time` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT '创建时间',
  `update_by` varchar(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NULL DEFAULT NULL COMMENT '更新者',
  `update_time` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP COMMENT '更新时间',
  `remark` varchar(500) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NULL DEFAULT NULL COMMENT '备注',
  `cloud_access_info` varchar(500) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NULL DEFAULT NULL COMMENT '上云接入信息（如TCP接入点、接入方式、协议等）',
  PRIMARY KEY (`id`) USING BTREE,
  INDEX `idx_prod`(`product_id` ASC) USING BTREE,
  INDEX `idx_key`(`point_key` ASC) USING BTREE
) ENGINE = InnoDB CHARACTER SET = utf8mb4 COLLATE = utf8mb4_0900_ai_ci COMMENT = '物联网—产品点位模板表' ROW_FORMAT = Dynamic;

-- ----------------------------
-- Records of iot_product_point
-- ----------------------------
INSERT INTO `iot_product_point` VALUES (12938, 1938422622099447810, '空气温度', 'ceshi', '物联点位', '数值', '℃', '0', 0, '0', '0', 1, 4, 16, 502, 'ABCD', 1, '读写', '全部存储', 0, '%s/10', '', '0', '0', NULL, '2025-07-09 10:17:54', NULL, '2025-07-09 18:00:00', '', NULL);
INSERT INTO `iot_product_point` VALUES (12939, 1938422622099447810, '空气湿度', 'shidu', '物联点位', '数值', '%', '0', 1, '0', '0', 1, 4, 16, 501, 'ABCD', 0, '读写', '全部存储', 0, '%s/10', '', '0', '0', NULL, '2025-07-09 10:17:54', NULL, '2025-07-09 18:00:00', '', NULL);
INSERT INTO `iot_product_point` VALUES (12940, 1938422622099447810, '风速', 'fengsu4', '物联点位', '数值', 'm/s', '0', 0, '0', '0', 4, 4, 16, 1, 'ABCD', 0, '读写', '全部存储', 0, '%s/10', '', '0', '0', NULL, '2025-07-09 10:17:54', NULL, '2025-07-09 18:00:00', '', NULL);
INSERT INTO `iot_product_point` VALUES (12941, 1938422622099447810, '风向', 'fengxiang', '物联点位', '文本', '', '', 0, '0', '0', 2, 4, 16, 1, 'ABCD', 0, '读写', '全部存储', 0, '', '', '0', '0', NULL, '2025-07-09 10:17:54', NULL, '2025-07-09 18:00:00', '', NULL);
INSERT INTO `iot_product_point` VALUES (12942, 1938422622099447810, '10CM土壤温度', 'turangwendu', '物联点位', '数值', '℃', '0', 0, '0', '0', 5, 4, 16, 2, 'ABCD', 1, '读写', '全部存储', 0, '%s/10', '', '0', '0', NULL, '2025-07-09 10:17:54', NULL, '2025-07-09 18:00:00', '', NULL);
INSERT INTO `iot_product_point` VALUES (12943, 1938422622099447810, '10CM土壤湿度', 'turangshidu', '物联点位', '数值', '%RH', '0', 0, '0', '0', 5, 4, 16, 1, 'ABCD', 0, '读写', '全部存储', 0, '%s/10', '', '0', '0', NULL, '2025-07-09 10:17:54', NULL, '2025-07-09 18:00:00', '', NULL);
INSERT INTO `iot_product_point` VALUES (12944, 1938422622099447810, '20CM土壤温度', 'turangwendu61', '物联点位', '数值', '℃', '0', 0, '0', '0', 6, 4, 16, 2, 'ABCD', 1, '读写', '全部存储', 0, '%s/10', '', '0', '0', NULL, '2025-07-09 10:17:54', NULL, '2025-07-09 18:00:00', '', NULL);
INSERT INTO `iot_product_point` VALUES (12945, 1938422622099447810, '20CM土壤湿度', 'turangwendu6', '物联点位', '数值', '%RH', '0', 0, '0', '0', 6, 4, 16, 1, 'ABCD', 0, '读写', '全部存储', 0, '%s/10', '', '0', '0', NULL, '2025-07-09 10:17:54', NULL, '2025-07-09 18:00:00', '', NULL);
INSERT INTO `iot_product_point` VALUES (12946, 1938422622099447810, '今日降雨量', 'jinrileijiyuliang', '物联点位', '数值', 'mm', '0', 0, '0', '0', 1, 4, 16, 1, 'ABCD', 0, '读写', '全部存储', 0, '', '', '0', '0', NULL, '2025-07-09 10:17:54', NULL, '2025-07-09 18:00:00', '', NULL);
INSERT INTO `iot_product_point` VALUES (12947, 1938422622099447810, '降雨强度', 'jiangyuqiangdu', '物联点位', '数值', 'mm/h', '0', 0, '0', '0', 1, 4, 16, 1, 'ABCD', 0, '读写', '全部存储', 0, '', '', '0', '0', NULL, '2025-07-09 10:17:54', NULL, '2025-07-09 18:00:00', '', NULL);
INSERT INTO `iot_product_point` VALUES (12949, 1938422622099447810, '累计降雨量', 'yuliang', '物联点位', '数值', 'mm', '0', 1, '0', '0', 3, 4, 16, 1, 'ABCD', 0, '读写', '全部存储', 0, '%s/10', '', '0', '0', NULL, '2025-07-09 10:17:54', NULL, '2025-07-09 18:00:00', '', NULL);
INSERT INTO `iot_product_point` VALUES (12950, 1938422622099447810, '昨日累计降雨量', 'zuorileijijiangyuliang', '物联点位', '数值', 'mm', '0', 0, '0', '0', 1, 4, 16, 1, 'ABCD', 0, '读写', '全部存储', 0, '', '', '0', '0', NULL, '2025-07-09 10:17:54', NULL, '2025-07-09 18:00:00', '', NULL);

SET FOREIGN_KEY_CHECKS = 1;
