 
 
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
   INDEX `idx_var_id`(`variable_id` ASC) USING BTREE
 ) ENGINE = InnoDB CHARACTER SET = utf8mb4 COLLATE = utf8mb4_0900_ai_ci COMMENT = '  是保存每个设备最新值的实体\r\n\r\n存储实时值：避免从历史表中聚合最新记录，查询效率更高。\r\n\r\n提供变量映射：按变量键快速找到变量 ID 以保存/更新数据。' ROW_FORMAT = Dynamic;
 
 -- ----------------------------
 -- Records of iot_device_variable
 -- ----------------------------

 INSERT INTO `iot_device_variable` VALUES (99100000000001, 99100001250627, 12938, NULL, NULL, '0', '0', NULL, '2025-07-13 00:08:47', NULL, '2025-07-13 00:08:47', NULL);
 INSERT INTO `iot_device_variable` VALUES (99100000000002, 99100001250627, 12939, NULL, NULL, '0', '0', NULL, '2025-07-13 00:08:47', NULL, '2025-07-13 00:08:47', NULL);
 INSERT INTO `iot_device_variable` VALUES (99100000000003, 99100001250627, 12940, '4660', '2025-07-13 00:09:30', '0', '0', NULL, '2025-07-13 00:08:47', NULL, '2025-07-13 00:09:29', NULL);
 INSERT INTO `iot_device_variable` VALUES (99100000000004, 99100001250627, 12941, NULL, NULL, '0', '0', NULL, '2025-07-13 00:08:47', NULL, '2025-07-13 00:08:47', NULL);
 INSERT INTO `iot_device_variable` VALUES (99100000000005, 99100001250627, 12942, '22136', '2025-07-13 00:09:30', '0', '0', NULL, '2025-07-13 00:08:47', NULL, '2025-07-13 00:09:29', NULL);
 INSERT INTO `iot_device_variable` VALUES (99100000000006, 99100001250627, 12943, NULL, NULL, '0', '0', NULL, '2025-07-13 00:08:47', NULL, '2025-07-13 00:08:47', NULL);
 INSERT INTO `iot_device_variable` VALUES (99100000000007, 99100001250627, 12944, NULL, NULL, '0', '0', NULL, '2025-07-13 00:08:47', NULL, '2025-07-13 00:08:47', NULL);
 INSERT INTO `iot_device_variable` VALUES (99100000000008, 99100001250627, 12945, NULL, NULL, '0', '0', NULL, '2025-07-13 00:08:47', NULL, '2025-07-13 00:08:47', NULL);
 INSERT INTO `iot_device_variable` VALUES (99100000000009, 99100001250627, 12946, NULL, NULL, '0', '1', NULL, '2025-07-13 00:08:47', NULL, '2025-07-13 09:10:43', NULL);
 INSERT INTO `iot_device_variable` VALUES (99100000000010, 99100001250627, 12947, NULL, NULL, '0', '1', NULL, '2025-07-13 00:08:47', NULL, '2025-07-13 09:10:43', NULL);
 INSERT INTO `iot_device_variable` VALUES (99100000000011, 99100001250627, 12949, NULL, NULL, '0', '1', NULL, '2025-07-13 00:08:47', NULL, '2025-07-13 09:10:43', NULL);
 INSERT INTO `iot_device_variable` VALUES (99100000000012, 99100001250627, 12950, NULL, NULL, '0', '1', NULL, '2025-07-13 00:08:47', NULL, '2025-07-13 09:10:43', NULL);
 
 SET FOREIGN_KEY_CHECKS = 1;
