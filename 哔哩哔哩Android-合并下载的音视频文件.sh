#!/bin/bash

basedir=`pwd`
now=`date +%Y%m%d%H%M%S`
ffmpeg_logfile="${basedir}/ffmpeg${now}.log"

# 第一级：脚本工作目录
dirstr=`ls -d */ | awk '{ 
	name=substr($1, 0, length($1)-1)
	if (match(name, /^[0123456789]+$/) > 0) {
		print name
	}
}'`
dirarr=(${dirstr})
echo "Work directory: ${basedir}"

for i in ${dirarr[@]}; do
	# 第二级：视频目录（可能会有多P情况）
	echo "Start processing: av${i}"
	cd "${basedir}/${i}"
	echo "[av${i}] Enter ${basedir}/${i}"

	inner_dirstr=`ls -d */ | awk '{ print substr($1, 0, length($1)-1) }'`
	inner_dirarr=(${inner_dirstr})
	for j in ${inner_dirarr[@]}; do
		# 第三级：分P目录
		echo "[av${i}] Start prossing: ${j}"
		cd "${basedir}/${i}/${j}"
		echo "[av${i}][${j}] Enter ${basedir}/${i}/${j}"

		# 获取视频标题，命名输出文件
		json_str=`awk 'BEGIN { FS=","; OFS="\n" } {
	       		for (i = 1; i <= NF; i++) {
				name=""
				if (index($i, "\"title\"") > 0) {
					name=substr($i, 10, length($i)-10)
				}

				if (index($i, "\"part\"") > 0) {
					name=substr($i, 9, length($i)-9)
				}

				if (name == "") {
					continue
				}

				gsub(/\\\/, "/", name)
				gsub(/\//, "⧸", name)
				gsub(/⧸⧸/, "⧸", name)
				gsub(/"/, "#", name)
				gsub(/\*/, "x", name)
				gsub(/</, "《", name)
				gsub(/>/, "》", name)
				gsub(/\?/, "？", name)
				gsub(/\|/, "丨", name)
				gsub(/:/, "：", name)
				print name
			}
		}' entry.json`
		OLD_IFS="$IFS"
		IFS='
'
		json_entry=(${json_str})
		#echo $json_star
		echo "[av${i}][${j}] Get video title, ok."

		file_name=''
		if [ ${#json_entry[0]} == ${#json_entry[1]} ] && [ "${json_entry[0]}" == "${json_entry[1]}" ]; then
			file_name="${i}_${json_entry[0]}"
		else
			file_name="${i}_${json_entry[0]} ${json_entry[1]}"
		fi
		#echo "${json_entry[0]} | ${json_entry[1]} | $i | $j"
		#echo $file_name
		echo "[av${i}][${j}] File name generated: \"${file_name}.mp4\""
		IFS="$OLD_IFS"

		# 第四级：ffmpeg工作目录（视频文件存放目录）
		target_dirstr=`ls -d */ | awk '{ print substr($1,0,length($1)-1) }'`
		target_dirarr=(${target_dirstr})
		cd "${basedir}/${i}/${j}/${target_dirarr[0]}"
		echo "[av${i}][${j}] Calling ffmpeg..."
		echo "============================================== [av${i}] ${file_name} ==============================================" >> "${ffmpeg_logfile}"
		ffmpeg -i video.m4s -i audio.m4s -c:v copy -c:a copy movie.mp4 >> "${ffmpeg_logfile}" 2>&1

		echo "[av${i}][${j}] Moving file..."
		file_path="${basedir}/${file_name}.mp4"
		mv movie.mp4 "${file_path}"
		#echo "${#file_path} | ${file_path}"

		echo "[av${i}] ${j} processing completed."
	done

	echo "av${i} prossing cpmpleted."
done



