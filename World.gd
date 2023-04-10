extends TileMap

class_name World

func _on_main_update_cell(x, y, isAlive):
	#print("cell "+ var_to_str(x)+" "+var_to_str(y)+"isAlive")
	var cell = Vector2i(x,y)
	if (isAlive):
		set_cell(0, cell,0,Vector2i.ZERO)
	else:
		erase_cell(0, cell)


func _on_main_clear_world():
	clear() # Replace with function body.
