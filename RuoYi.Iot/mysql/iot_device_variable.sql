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

 Date: 13/07/2025 09:17:51
*/

SET NAMES utf8mb4;
SET FOREIGN_KEY_CHECKS = 0;

-- ----------------------------
-- Table structure for iot_device_variable
-- ----------------------------
DROP TABLE IF EXISTS `iot_device_variable`;
CREATE TABLE `iot_device_variable`  (
  `id` bigint(20) NOT NULL COMMENT '主键',
  `device_id` bigint(20) NOT NULL COMMENT '设备ID，关联 t_iot_device.id',
  `variable_id` bigint(20) NOT NULL COMMENT '变量ID',
  `variable_name` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL COMMENT '变量名称',
  `variable_key` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL COMMENT '变量标识',
  `variable_type` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL COMMENT '变量类型（例如：物联点位/物联属性等）',
  `current_value` varchar(200) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NULL DEFAULT NULL COMMENT '当前值',
  `last_update_time` datetime NULL DEFAULT NULL COMMENT '更新时间',
  `status` varchar(1) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL DEFAULT '0' COMMENT '状态（0正常 1停用）',
  `del_flag` varchar(1) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL DEFAULT '0' COMMENT '删除标志（0存在 2删除）',
  `create_by` varchar(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NULL DEFAULT NULL COMMENT '创建者',
  `create_time` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT '创建时间',
  `update_by` varchar(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NULL DEFAULT NULL COMMENT '更新者',
  `update_time` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP COMMENT '更新时间',
  `remark` varchar(500) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NULL DEFAULT NULL COMMENT '备注',
  PRIMARY KEY (`id`) USING BTREE,
  INDEX `idx_device`(`device_id` ASC) USING BTREE,
  INDEX `idx_var_key`(`variable_key` ASC) USING BTREE,
  INDEX `idx_var_id`(`variable_id` ASC) USING BTREE
) ENGINE = InnoDB CHARACTER SET = utf8mb4 COLLATE = utf8mb4_0900_ai_ci COMMENT = '  是保存每个设备最新值的实体\r\n\r\n存储实时值：避免从历史表中聚合最新记录，查询效率更高。\r\n\r\n提供变量映射：按变量键快速找到变量 ID 以保存/更新数据。' ROW_FORMAT = Dynamic;

-- ----------------------------
-- Records of iot_device_variable
-- ----------------------------
INSERT INTO `iot_device_variable` VALUES (99100000000001, 99100001250627, 12938, '空气温度', 'ceshi', '物联点位', NULL, NULL, '0', '0', NULL, '2025-07-13 00:08:47', NULL, '2025-07-13 00:08:47', NULL);
INSERT INTO `iot_device_variable` VALUES (99100000000002, 99100001250627, 12939, '空气湿度', 'shidu', '物联点位', NULL, NULL, '0', '0', NULL, '2025-07-13 00:08:47', NULL, '2025-07-13 00:08:47', NULL);
INSERT INTO `iot_device_variable` VALUES (99100000000003, 99100001250627, 12940, '风速', 'fengsu4', '物联点位', '4660', '2025-07-13 00:09:30', '0', '0', NULL, '2025-07-13 00:08:47', NULL, '2025-07-13 00:09:29', NULL);
INSERT INTO `iot_device_variable` VALUES (99100000000004, 99100001250627, 12941, '风向', 'fengxiang', '物联点位', NULL, NULL, '0', '0', NULL, '2025-07-13 00:08:47', NULL, '2025-07-13 00:08:47', NULL);
INSERT INTO `iot_device_variable` VALUES (99100000000005, 99100001250627, 12942, '10CM土壤温度', 'turangwendu', '物联点位', '22136', '2025-07-13 00:09:30', '0', '0', NULL, '2025-07-13 00:08:47', NULL, '2025-07-13 00:09:29', NULL);
INSERT INTO `iot_device_variable` VALUES (99100000000006, 99100001250627, 12943, '10CM土壤湿度', 'turangshidu', '物联点位', NULL, NULL, '0', '0', NULL, '2025-07-13 00:08:47', NULL, '2025-07-13 00:08:47', NULL);
INSERT INTO `iot_device_variable` VALUES (99100000000007, 99100001250627, 12944, '20CM土壤温度', 'turangwendu61', '物联点位', NULL, NULL, '0', '0', NULL, '2025-07-13 00:08:47', NULL, '2025-07-13 00:08:47', NULL);
INSERT INTO `iot_device_variable` VALUES (99100000000008, 99100001250627, 12945, '20CM土壤湿度', 'turangwendu6', '物联点位', NULL, NULL, '0', '0', NULL, '2025-07-13 00:08:47', NULL, '2025-07-13 00:08:47', NULL);
INSERT INTO `iot_device_variable` VALUES (99100000000009, 99100001250627, 12946, '今日降雨量', 'jinrileijiyuliang', '物联点位', NULL, NULL, '0', '1', NULL, '2025-07-13 00:08:47', NULL, '2025-07-13 09:10:43', NULL);
INSERT INTO `iot_device_variable` VALUES (99100000000010, 99100001250627, 12947, '降雨强度', 'jiangyuqiangdu', '物联点位', NULL, NULL, '0', '1', NULL, '2025-07-13 00:08:47', NULL, '2025-07-13 09:10:43', NULL);
INSERT INTO `iot_device_variable` VALUES (99100000000011, 99100001250627, 12949, '累计降雨量', 'yuliang', '物联点位', NULL, NULL, '0', '1', NULL, '2025-07-13 00:08:47', NULL, '2025-07-13 09:10:43', NULL);
INSERT INTO `iot_device_variable` VALUES (99100000000012, 99100001250627, 12950, '昨日累计降雨量', 'zuorileijijiangyuliang', '物联点位', NULL, NULL, '0', '1', NULL, '2025-07-13 00:08:47', NULL, '2025-07-13 09:10:43', NULL);

SET FOREIGN_KEY_CHECKS = 1;
