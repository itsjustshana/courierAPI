SET @gopak_client_id = (
  SELECT id FROM clients
  WHERE company_name = 'Gopak'
  LIMIT 1
);

INSERT INTO users (
  client_id, username, password_hash, first_name, last_name,
  full_name, email, mobile, home_phone, id_type, id_number,
  pickup_location, address_1, address_2, city, parish,
  normalized_email, role, is_active
) VALUES
(@gopak_client_id, 'rayon-mcghan1', '$2y$10$pHHNFsjZtBAOsZLN3hyV/.03WKmQc.QnCDaJUqta9PSx0.G/7S5FK', 'Rayon', 'Mcghan1', 'Rayon Mcghan1', 'rayon.shawn@gmail.com1', '8765332929', NULL, 'Driver''s License', 'rayon', 'The Auto House', '101 First Street', 'Newport West', 'Kingston', 'Kingston', 'RAYON.SHAWN@GMAIL.COM1', 'Customer', 1),
(@gopak_client_id, 'shaniqua', '$2y$10$7EFNicAE/Ltn65jFSQsOLeQzgoM5dHNafUVhZuONcTnXNVspzl7SK', 'Shaniqua', 'Trusty', 'Shaniqua Trusty', 'shaniqua.trusty@gmail.com', '8765380370', NULL, 'Passport No', 'A6661985', 'The Auto House', 'Gordons Close', '16', 'Bull Bay', 'St. Andrew', 'SHANIQUA.TRUSTY@GMAIL.COM', 'Customer', 1),
(@gopak_client_id, 'shanakay-campbell', '$2y$10$tnQYC7mTs0qR8gFYvE70SuiVFsbhU.BaljcHAegBfQC9ZiR7bEPqe', 'Shanakay', 'Campbell', 'Shanakay Campbell', 'shanakay.a.campbell@gmail.com', '8764071466', '8769695658', 'Driver''s License', '123456', 'The Auto House', 'test', 'test', 'Kingston', 'Kingston', 'SHANAKAY.A.CAMPBELL@GMAIL.COM', 'Customer', 1),
(@gopak_client_id, 'ashlie-gaye', '$2y$10$kJ5ZxDUjde4nvJMve3Ud5uH8kd.iQGYgt9Zwvp9W6FlopP1/x6RJm', 'ASHLIE-GAYE', 'PEARSON', 'ASHLIE-GAYE PEARSON', 'ASHLIEAPEARSON@YAHOO.COM', '8763540906', NULL, 'Passport No', 'A3996153', 'The Auto House', '20 DUHANEY DRIVE', 'KINGSTON 20', 'KINGSTON', 'St. Andrew', 'ASHLIEAPEARSON@YAHOO.COM', 'Customer', 1),
(@gopak_client_id, 'stacy-ann', '$2y$10$r3BWXgRbuPaTBVxgtth7wu.eu42VezVjn7L34k0OYFmwIW1fE/wN6', 'Stacy-Ann', 'Watson', 'Stacy-Ann Watson', 'stacyannwatson7@gmail.com', '8763462567', NULL, 'Driver''s License', '120185067', 'The Auto House', 'Taylor Land', 'Nine Miles', 'Bull Bay', 'St. Andrew', 'STACYANNWATSON7@GMAIL.COM', 'Customer', 1),
(@gopak_client_id, 'shawna-kay', '$2y$10$7ws5fOh2y3Cuncg.OtJfieJjHucrfyobbbPvZpVWHdK/U4bjJT2z2', 'Shawna-Kay', 'Brown', 'Shawna-Kay Brown', 'kaymarian@gmail.com', '8763519519', NULL, 'Driver''s License', '121748103', 'The Auto House', 'Copacabana', 'Bull Bay', 'St Andrew', 'Kingston', 'KAYMARIAN@GMAIL.COM', 'Customer', 1),
(@gopak_client_id, 'trefiena', '$2y$10$59gPWoFGZV.ANZFpcqP3nO4CBwBkF3nITDEsImNQhpK5WnRvsTNrK', 'Trefiena', 'Scott', 'Trefiena Scott', 'trefiena@yahoo.com', '8762699701', NULL, 'Driver''s License', '117569119', 'The Auto House', 'lot 982, 2 east greater portmore', NULL, 'st. Catherine', 'St. Catherine', 'TREFIENA@YAHOO.COM', 'Customer', 1),
(@gopak_client_id, 'ava-gay', '$2y$10$/wgT7cgELkmR/8JkNwl1TOISXieV3vRkiiAAOTYZsBvI/mY/noAey', 'Ava-Gay', 'Robinson', 'Ava-Gay Robinson', 'avagay_robinson10@yahoo.com', '876-394-7288', NULL, 'Driver''s License', '120476738', 'The Auto House', 'Lot 118', '4 East', 'Portmore', 'St. Catherine', 'AVAGAY_ROBINSON10@YAHOO.COM', 'Customer', 1),
(@gopak_client_id, 'sehesha', '$2y$10$hGqrMmA.j0uqfm17L457Ne3S6LEaDiqGjCFcUWIx9WdoFN24wtxfa', 'Sehesha', 'Mckenzie', 'Sehesha Mckenzie', 'seheshamckenzie@gmail.com', '876-336-7800', NULL, 'Driver''s License', '112817009', 'The Auto House', '131 Sandstone Drive Hellshire Glades', NULL, 'St Catherine', 'St. Catherine', 'SEHESHAMCKENZIE@GMAIL.COM', 'Customer', 1),
(@gopak_client_id, 'shanay', '$2y$10$yMrgHQQeGYfRZ9G7FayCyek6UxdIQHqUag/JDlccnpBdDOAl83aQG', 'Shanay', 'Atherton', 'Shanay Atherton', 'shanayatherton@rocketmail.com', '8765820809', NULL, 'Other', '40711378', 'The Auto House', '101 First Street', 'Newport West', 'Kingston 13', 'St. Andrew', 'SHANAYATHERTON@ROCKETMAIL.COM', 'Customer', 1),
(@gopak_client_id, 'dania', '$2y$10$Cuc.dTjOyvmljs8Z7ZhqweHIZEkADVT0EBy9tZclOPfgzixC2TMom', 'DANIA', 'SMITH', 'DANIA SMITH', 'daniasmith95@yahoo.com', '8767711552', NULL, 'Other', '2366414', 'The Auto House', 'Edgewater, Portmore', NULL, 'St.Catherine', 'St. Catherine', 'DANIASMITH95@YAHOO.COM', 'Customer', 1),
(@gopak_client_id, 'venesa', '$2y$10$4TOf7qaQHhp9ffCwuzQxeOoPFCxA2vlAMTzE90SOZtMs32l8fumri', 'Venesa', 'Kelly', 'Venesa Kelly', 'kelly.venesa.vk@gmail.com', '8768444007', NULL, 'Other', '2207413', 'The Auto House', '66 Salkey Avenue, Duhaney Park', NULL, 'Kingston 20', 'Kingston', 'KELLY.VENESA.VK@GMAIL.COM', 'Customer', 1),
(@gopak_client_id, 'sonia', '$2y$10$Q/LoikqcMmLuXYfbWi8vGuGb/2495VMoD7dEZa2kkblHGu6hV0Hk.', 'Sonia', 'Wilkins', 'Sonia Wilkins', 'sonia.e.wilkins@gmail.com', '8763783522', NULL, 'Other', '1234', 'The Auto House', '22 Woodlawn Avenue', NULL, 'Kingston 19', 'Kingston', 'SONIA.E.WILKINS@GMAIL.COM', 'Customer', 1),
(@gopak_client_id, 'adiasha-gordon1', '$2y$10$W.vttId5XBN9n2OiENUiP.7RHqhAGhoa79BPR8nUG9pDBokV760oi', 'Adiasha', 'Gordon1', 'Adiasha Gordon1', 'adiashagordon49@gmail.com1', '8765957656', NULL, 'Passport No', 'A6165363', 'The Auto House', '17 Lyndhurst Road Kingston 5', NULL, 'Kingston', 'Kingston', 'ADIASHAGORDON49@GMAIL.COM1', 'Customer', 1),
(@gopak_client_id, 'dasha', '$2y$10$6QvWTBL2B5.B.66/h1eczeBV0W8/5qyZXUdqYMAXJtA3ALZxjbTGe', 'Dasha', 'dewar', 'Dasha dewar', 'dashadewar2005@gmail.com', '8764874705', NULL, 'Other', '41400607', 'The Auto House', '529 3 east greater portmore', NULL, 'Portmore', 'St. Catherine', 'DASHADEWAR2005@GMAIL.COM', 'Customer', 1),
(@gopak_client_id, 'kellian', '$2y$10$PbK4C092CnQ6maWvFoqTDODC4UWVoPB4VwHZFf8OTpGNjrc6SPb.S', 'Kellian', 'Taylor', 'Kellian Taylor', 'taylorkjellyann@yahoo.com', '8765700394', NULL, 'Driver''s License', '122636651', 'The Auto House', 'Windsor Forest', 'Windsor Forest P.A', 'Portland', 'Portland', 'TAYLORKJELLYANN@YAHOO.COM', 'Customer', 1),
(@gopak_client_id, 'cadain', '$2y$10$RMgaRsPmc1SvRaUXr4W4dOZRkkl7LQ4HTdQwmNtJG0yQialLlIV76', 'Cadain', 'Miller', 'Cadain Miller', 'cadainmiller@gmail.com', '8764080998', NULL, 'Driver''s License', '125578059', 'The Auto House', 'Moore Park', NULL, 'Montego Bay', 'St. James', 'CADAINMILLER@GMAIL.COM', 'Customer', 1),
(@gopak_client_id, 'shanakay-user', '$2y$10$Uz0juYY.qA0X7xnMEp10CeILMZlQarJJ1hpaVlZLWZkj4XYcd8WKS', 'Shanakay', 'User', 'Shanakay User', 'shanakay@syntaxdatasolutions.com', '8764071466', '9996636', 'Driver''s License', '123456', 'The Auto House', 'Address1', 'Address2', 'Kingston', 'Kingston', 'SHANAKAY@SYNTAXDATASOLUTIONS.COM', 'Customer', 1),
(@gopak_client_id, 'shanakay-admin', '$2y$10$rbVbBveV.z2RoIQwmOt45eoW6QOfyrP3KxYihns6riX4KzrMsp3Ky', 'Shanakay', 'Admin', 'Shanakay Admin', 'shanakayadmin@syntaxdatasolutions.com', '963', '963', 'Driver''s License', '123456', 'The Auto House', 'Fgr', 'wer', 'wer', 'St. Thomas', 'SHANAKAYADMIN@SYNTAXDATASOLUTIONS.COM', 'Customer', 1),
(@gopak_client_id, 'jane', '123', 'Jane', 'Brown', 'Jane Brown', 'jane@gmail.com', '8768881234', NULL, 'Driver''s License', '12345678', 'Kingston', '123 Demo Avenue', NULL, 'Kingston', 'Kingston', 'JANE@GMAIL.COM', 'Customer', 1),
(@gopak_client_id, 'orlando', '123', 'Orlando', 'Williams', 'Orlando Williams', 'orlando', '8768881234', NULL, 'Driver''s License', '12345678', 'Kingston', '123 Demo Avenue', NULL, 'Kingston', 'Kingston', NULL, 'Customer', 1),
(@gopak_client_id, 'janet', '$2y$10$pnOs.7bk.68lLSVZaOnGv.ohlKLMZ.TdFN4fMvk/tYK2WXmBnUuha', 'Janet', 'Shuballiee', 'Janet Shuballiee', 'campbelljanet215@gmail.com', '8764616650', NULL, 'Driver''s License', '000000', 'The Auto House', '16 Wallens Road', 'Westchester', 'Portmore', 'St. Catherine', 'CAMPBELLJANET215@GMAIL.COM', 'Customer', 1),
(@gopak_client_id, 'no', 'blank', '-No', 'Name Assigned-', '-No Name Assigned-', 'blank', '8768881234', NULL, 'Driver''s License', '12345678', 'Kingston', '123 Demo Avenue', NULL, 'Kingston', 'Kingston', NULL, 'Customer', 1),
(@gopak_client_id, 'aldeyshein', '$2y$10$jVjY.Blc62FYZNSNfkch7e/LcDk7.tyPGIhNqsN1ZhX9l10873fAK', 'Aldeyshein', 'Grant', 'Aldeyshein Grant', 'aldeyshein18@gmail.com', '8768993728', '8868993728', 'Driver''s License', '3477468', 'The Church of God Sanctified Kingston', '131 Barbican Road', 'Kingston 8', 'Barbican', 'St. Andrew', 'ALDEYSHEIN18@GMAIL.COM', 'Customer', 1),
(@gopak_client_id, 'adiasha-gordon', '$2y$10$RN0Ubchk7O3tBRjn6.dTK.Ju88rcH3IoynOBvby4uWVqJ59MbnUJO', 'Adiasha', 'Gordon', 'Adiasha Gordon', 'adiashagordon49@gmail.com', NULL, NULL, 'Driver''s License', NULL, 'The Auto House', NULL, NULL, NULL, NULL, 'ADIASHAGORDON49@GMAIL.COM', 'Customer', 1),
(@gopak_client_id, 'kristoff', '$2y$10$dToGPuiN/YCwQ857M.APFeU5y4m6lgmxWlfkfxGD/G9WsyLCGa6iu', 'Kristoff', 'Russell', 'Kristoff Russell', 'kristoffrussell@gmail.com', '8763658201', '8763937521', 'Driver''s License', '126984271', 'The Auto House', 'Lot 70 Butter Cup Ave', NULL, 'Spanish Town', 'St. Catherine', 'KRISTOFFRUSSELL@GMAIL.COM', 'Customer', 1),
(@gopak_client_id, 'adrian', '$2y$10$RMMj0wAd2qi/h98sdmPD9elVLzRlD8zw38myOjFwbsj2QyaIVZtfy', 'Adrian', 'smith', 'Adrian smith', 'adriangodsonsmith@gmail.com', '18764459494', NULL, 'Driver''s License', '123667054', 'Knutsford Branch', '56 Melbrook heights Kingston 17', NULL, 'Kingston', 'Kingston', 'ADRIANGODSONSMITH@GMAIL.COM', 'Customer', 1),
(@gopak_client_id, 'rayon-mcghan', '$2y$10$y973pwIYolB6Qp0VETBQmeDR4XpenH9R7JTa.XEsFrOf0UysGqFwu', 'Rayon', 'Mcghan', 'Rayon Mcghan', 'rayon.shawn@gmail.com', '8765332929', NULL, 'Driver''s License', NULL, 'The Auto House', NULL, NULL, NULL, NULL, 'RAYON.SHAWN@GMAIL.COM', 'Customer', 1),
(@gopak_client_id, 'shadeen', '$2y$10$r/OsBkgb6CKNA7k1FJ3z/ObqOoXn1FRtxHKkuMGHKECR3.COPgTPq', 'shadeen', NULL, 'shadeen', 'solomon', '8765533645', NULL, 'Driver''s License', '124542506', 'The Auto House', 'kingston', 'kingston', 'kingston', 'Kingston', NULL, 'Customer', 1),
(@gopak_client_id, 'shadeen-solomon', '$2y$10$aXaP0.LNX3hMbj9Ps2XXM.sXrthTUqbGBF6D3BewbMJCxRlwVjNke', 'Shadeen', 'Solomon', 'Shadeen Solomon', 'SHADSOLOMON@HOTMAIL.COM', '8765533645', NULL, 'Driver''s License', '124542506', 'The Auto House', 'KINGSTON', 'KINGSTON', 'KINGSTON', 'Kingston', 'SHADSOLOMON@HOTMAIL.COM', 'Customer', 1),
(@gopak_client_id, 'kerrian', '$2y$10$gpOxCY9s0nSX7Otzesjlge2WvUPqVFrU4BJJYpze675zTsGE6ahoC', 'Kerrian', 'Chambers', 'Kerrian Chambers', 'gady25kc@gmail.com', '876 509-2573', '876', 'Driver''s License', '118996878', 'The Auto House', '961 St. Claire Close', 'Green Acres', 'Spanish Town', 'St. Catherine', 'GADY25KC@GMAIL.COM', 'Customer', 1),
(@gopak_client_id, 'shanakay-overseer', '$2y$10$y1.PFkLy1G6S2lWBlFeNeuQceadIqjV5m0c7Jwis.tjh8pJHihWEO', 'Shanakay', 'Overseer', 'Shanakay Overseer', 'shanakay@test.com', '4071466', '68', 'Driver''s License', '233333', 'The Auto House', '22 Woodlawn Avenue', 'Kingston 19', 'Kingston', 'Trelawny', 'SHANAKAY@TEST.COM', 'Customer', 1),
(@gopak_client_id, 'nickiesha', '$2y$10$qrtLAqSYtAU6B8jIQBcKB.csgV2FeYk90QjdBJ6tPmRuprYdZDHSC', 'Nickiesha', 'Francis', 'Nickiesha Francis', 'nickiesha@hotmail.com', '8763489993', NULL, 'Driver''s License', '3271716', 'The Auto House', '15 Eltham Boulvard', '101 First Street Newport West', 'St Catherine', 'St. Catherine', 'NICKIESHA@HOTMAIL.COM', 'Customer', 1),
(@gopak_client_id, 'clive', '$2y$10$Uy.Z5pK1.xawVjFAajQq7ex6fZTv4oh3fD66KwWS4RlWs/BBWOy5O', 'Clive', 'Smith', 'Clive Smith', 'csa.christlike@gmail.com', NULL, NULL, 'Driver''s License', NULL, NULL, NULL, NULL, NULL, NULL, 'CSA.CHRISTLIKE@GMAIL.COM', 'Customer', 1),
(@gopak_client_id, 'natasha', '$2y$10$Vhr1AwbbP1mIgazPjJqrG.9MYji37P4SKEDX.4hdPYqwdf4HZ/bTa', 'Natasha', 'Mills', 'Natasha Mills', 'tashamills2005@hotmail.com', NULL, NULL, 'Driver''s License', NULL, NULL, NULL, NULL, NULL, NULL, NULL, 'Customer', 1)
ON DUPLICATE KEY UPDATE
  client_id = VALUES(client_id),
  password_hash = VALUES(password_hash),
  first_name = VALUES(first_name),
  last_name = VALUES(last_name),
  full_name = VALUES(full_name),
  email = VALUES(email),
  mobile = VALUES(mobile),
  home_phone = VALUES(home_phone),
  id_type = VALUES(id_type),
  id_number = VALUES(id_number),
  pickup_location = VALUES(pickup_location),
  address_1 = VALUES(address_1),
  address_2 = VALUES(address_2),
  city = VALUES(city),
  parish = VALUES(parish),
  normalized_email = VALUES(normalized_email),
  role = VALUES(role),
  is_active = VALUES(is_active),
  updated_at = CURRENT_TIMESTAMP(6);

