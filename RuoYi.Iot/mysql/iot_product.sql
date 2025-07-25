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

 Date: 12/07/2025 18:30:24
*/

SET NAMES utf8mb4;
SET FOREIGN_KEY_CHECKS = 0;

-- ----------------------------
-- Table structure for iot_product
-- ----------------------------
DROP TABLE IF EXISTS `iot_product`;
CREATE TABLE `iot_product`  (
  `id` bigint(20) NOT NULL COMMENT '主键',
  `product_name` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL COMMENT '产品名称',
  `org_id` bigint(20) NOT NULL COMMENT '所属组织ID',
  `product_model` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL COMMENT '产品型号',
  `product_code` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL COMMENT '产品编码',
  `brand_name` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL COMMENT '品牌名称',
  `network_protocol` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL COMMENT '联网方式（，1 表示蜂窝数据2/3/4/5G，2 表示以太网，3 表示 WiFi，4 表示 NB-IoT，5 表示串口。）',
  `access_protocol` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL COMMENT '接入协议（1 表示 TCP，2 表示 MQTT，3 表示 HTTPS，4 表示 LwM2M，5 表示 LoRaWan，6 表示通过网关。）',
  `data_protocol` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL COMMENT '数据协议（1 表示 Modbus RTU，2 表示 Modbus TCP，3 表示 DL/T645-1997，4 表示 DL/T645-2007，5 表示 DL/T698.45-2017，6 表示 JSON，7 表示数据透传，8 表示自定义 Zigbee 协议，9 表示 CJ/T188-2004。）',
  `is_shared` tinyint(4) NOT NULL DEFAULT 0 COMMENT '是否共享（0否 1是）',
  `description` varchar(500) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NULL DEFAULT NULL COMMENT '产品描述',
  `status` varchar(1) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL DEFAULT '0' COMMENT '状态（0正常 1停用）',
  `del_flag` varchar(1) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL DEFAULT '0' COMMENT '删除标志（0存在 2删除）',
  `create_by` varchar(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NULL DEFAULT NULL COMMENT '创建者',
  `create_time` datetime NULL DEFAULT CURRENT_TIMESTAMP COMMENT '创建时间',
  `update_by` varchar(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NULL DEFAULT NULL COMMENT '更新者',
  `update_time` datetime NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP COMMENT '更新时间',
  `remark` varchar(500) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NULL DEFAULT NULL COMMENT '备注',
  PRIMARY KEY (`id`) USING BTREE,
  INDEX `idx_org`(`org_id` ASC) USING BTREE,
  INDEX `idx_model`(`product_model` ASC) USING BTREE,
  INDEX `idx_code`(`product_code` ASC) USING BTREE
) ENGINE = InnoDB CHARACTER SET = utf8mb4 COLLATE = utf8mb4_0900_ai_ci COMMENT = '物联网—产品表' ROW_FORMAT = Dynamic;

-- ----------------------------
-- Records of iot_product
-- ----------------------------
INSERT INTO `iot_product` VALUES (1938422622099447810, '透传通讯', 1869569731608997890, 'G771', '99630683', '有人G771', '1', '1', '1', 0, NULL, '0', '0', NULL, '2025-06-27 10:22:22', NULL, NULL, '');

SET FOREIGN_KEY_CHECKS = 1;
